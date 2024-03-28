using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models.Modmail;
using Nellebot.Common.Utils;
using Nellebot.Data.Repositories;
using Nellebot.Helpers;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class RequestModmailTicketHandler : IRequestHandler<RequestModmailTicketCommand>
{
    private const string RealNameButtonId = "realNameButton";
    private const string PseudonymButtonId = "pseudonymButton";
    private const string CancelButtonId = "cancelButton";
    private const string CancelMessageToken = "cancel";
    private readonly ModmailTicketRepository _modmailTicketRepo;

    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;

    public RequestModmailTicketHandler(
        IOptions<BotOptions> options,
        DiscordResolver resolver,
        ModmailTicketRepository modmailTicketRepo)
    {
        _options = options.Value;
        _resolver = resolver;
        _modmailTicketRepo = modmailTicketRepo;
    }

    public async Task Handle(RequestModmailTicketCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var channel = ctx.Channel;
        var requestMessage = request.RequestMessage;

        var member = await _resolver.ResolveGuildMember(ctx.User.Id);

        if (member is null)
        {
            var isDmFromNonGuildMember = channel.IsPrivate;

            if (isDmFromNonGuildMember)
            {
                var messageContent = "Members only!";

                await channel.SendMessageAsync(messageContent);

                return;
            }

            throw new Exception("Couldn't fetch member");
        }

        var existingTicket = await _modmailTicketRepo.GetActiveTicketByRequesterId(ctx.User.Id, cancellationToken);

        if (existingTicket != null)
        {
            var messageContent = "You already have an open ticket! Just type your message here and I will pass it on.";

            await member.SendMessageAsync(messageContent);

            return;
        }

        var choice = await CollectIdentityChoice(member, cancellationToken);

        if (choice == CancelButtonId)
        {
            await HandleCancellation(member, null, cancellationToken);

            return;
        }

        var requesterIsAnonymous = choice == PseudonymButtonId;

        string requesterDisplayName;

        if (!requesterIsAnonymous)
        {
            requesterDisplayName = member.DisplayName;
        }
        else
        {
            requesterDisplayName = PseudonymGenerator.GetRandomPseudonym();
        }

        var stub = await CreateStubModmailTicket(ctx, requesterDisplayName, requesterIsAnonymous, cancellationToken);

        await member.SendMessageAsync($"Understood. Your message will be sent as **{requesterDisplayName}**.");

        DiscordMessage messageToRelay;

        if (requestMessage is not null)
        {
            var confirmed = await CollectRequestMessageConfirmation(requestMessage, member);

            if (!confirmed)
            {
                await HandleCancellation(member, stub, cancellationToken);

                return;
            }

            messageToRelay = requestMessage;
        }
        else
        {
            var collectedMessage = await CollectRequestMessage(member);

            if (collectedMessage is null ||
                collectedMessage.Content.Equals(CancelMessageToken, StringComparison.InvariantCultureIgnoreCase))
            {
                await HandleCancellation(member, stub, cancellationToken);

                return;
            }

            messageToRelay = collectedMessage;
        }

        await PostStubModmailTicket(stub, messageToRelay, cancellationToken);

        await messageToRelay.CreateSuccessReactionAsync();

        var ticketCreateMessage = """
                                  Thanks! A staff member will get back to you soon.
                                  If there is something you wish to add do so by sending a new message as I won't be checking for updates to your messages.
                                  """;

        await member.SendMessageAsync(ticketCreateMessage);
    }

    /// <summary>
    ///     Collect a request message if the user didn't provide one in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage" />.</returns>
    private static async Task<DiscordMessage?> CollectRequestMessage(DiscordMember member)
    {
        var messageContent =
            "Please type your message here and I will pass it on. If you want to cancel just type `cancel`.";

        var promptForMessageMessage = await member.SendMessageAsync(messageContent);

        var promptInteractivityResult = await promptForMessageMessage.Channel.GetNextMessageAsync(member);

        if (promptInteractivityResult.TimedOut)
        {
            return null;
        }

        return promptInteractivityResult.Result;
    }

    /// <summary>
    ///     Collect confirmation from user if they provided a request message in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage" />.</returns>
    private static async Task<bool> CollectRequestMessageConfirmation(DiscordMessage message, DiscordMember member)
    {
        var messageContent = $"""
                              The following message will be sent to the moderators.
                              {message.GetQuotedContent()}
                              Please react with {EmojiMap.WhiteCheckmark} to confirm or {EmojiMap.RedX} to cancel.
                              """;

        var promptForConfirmMessage = await member.SendMessageAsync(messageContent);

        await promptForConfirmMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        var promptInteractivityResult = await promptForConfirmMessage.WaitForReactionAsync(member);

        if (promptInteractivityResult.TimedOut)
        {
            return false;
        }

        var choice = promptInteractivityResult.Result.Emoji.Name;

        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        return choice == EmojiMap.WhiteCheckmark;
    }

    /// <summary>
    ///     Collect choice of identity from user i.e. use real name or pseudonym.
    /// </summary>
    /// <returns>Returns true if the user chooses to be anonymous.</returns>
    private static async Task<string> CollectIdentityChoice(DiscordMember member, CancellationToken cancellationToken)
    {
        var realName = member.DisplayName;

        var introMessageContent = $"""
                                   Hello and welcome to Modmail!
                                   Would you like to send the message as **{realName}** or do you want to use a random pseudonym?
                                   """;

        var realNameButton = new DiscordButtonComponent(ButtonStyle.Primary, RealNameButtonId, realName);
        var pseudonymButton = new DiscordButtonComponent(ButtonStyle.Primary, PseudonymButtonId, "Random pseudonym");
        var cancelButton = new DiscordButtonComponent(ButtonStyle.Secondary, CancelButtonId, "Cancel");

        var introMessageBuilder = new DiscordMessageBuilder()
            .WithContent(introMessageContent)
            .AddComponents(realNameButton, pseudonymButton, cancelButton);

        var introMessage = await member.SendMessageAsync(introMessageBuilder);

        var interactionResult = await introMessage.WaitForButtonAsync(cancellationToken);

        if (interactionResult.TimedOut)
        {
            return CancelButtonId; // Assume cancelled
        }

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(
                                                                       InteractionResponseType.UpdateMessage,
                                                                       choiceInteractionResponseBuilder);

        return interactionResult.Result.Id;
    }

    private async Task HandleCancellation(
        DiscordMember member,
        ModmailTicket? stub = null,
        CancellationToken cancellationToken = default)
    {
        if (stub != null) await CancelStubModmailTicket(stub, cancellationToken);

        await member.SendMessageAsync("Understood. You can always request a ticket again later.");
    }

    private Task<ModmailTicket> CreateStubModmailTicket(
        BaseContext ctx,
        string requesterDisplayName,
        bool requesterIsAnonymous,
        CancellationToken cancellationToken)
    {
        var modmailTicket = new ModmailTicket
        {
            IsAnonymous = requesterIsAnonymous,
            RequesterId = ctx.User.Id,
            RequesterDisplayName = requesterDisplayName,
        };

        return _modmailTicketRepo.CreateTicket(modmailTicket, cancellationToken);
    }

    private Task CancelStubModmailTicket(ModmailTicket ticket, CancellationToken cancellationToken)
    {
        return _modmailTicketRepo.DeleteTicket(ticket, cancellationToken);
    }

    private async Task PostStubModmailTicket(
        ModmailTicket ticket,
        DiscordMessage requestMesage,
        CancellationToken cancellationToken)
    {
        var modmailChannelId = _options.ModmailChannelId;

        var modmailChannel = (DiscordForumChannel)_resolver.ResolveGuild().Channels[modmailChannelId];

        var modmailPostTitle = ticket.IsAnonymous
            ? $"Ticket from anonymous user {ticket.RequesterDisplayName}"
            : $"Ticket from server member {ticket.RequesterDisplayName}";

        var relayMessageContent = $"""
                                   {ticket.RequesterDisplayName} says
                                   {requestMesage.GetQuotedContent()}
                                   """;

        var modmailPostMessage = new DiscordMessageBuilder()
            .WithContent(relayMessageContent);

        var fpBuilder = new ForumPostBuilder()
            .WithName(modmailPostTitle)
            .WithMessage(modmailPostMessage);

        // Refresh the ticket in case it was updated while waiting for the user to type their message.
        var refreshedTicket =
            await _modmailTicketRepo.GetActiveTicketByRequesterId(ticket.RequesterId, cancellationToken)
            ?? throw new Exception("Ticket's gone");

        if (refreshedTicket.TicketPost != null)
        {
            throw new Exception("Ticket already posted");
        }

        var forumPost = await modmailChannel.CreateForumPostAsync(fpBuilder);

        var postedTicket = refreshedTicket with
        {
            TicketPost = new ModmailTicketPost(forumPost.Channel.Id, forumPost.Message.Id),
        };

        _ = await _modmailTicketRepo.UpdateTicketPost(postedTicket, cancellationToken);
    }
}
