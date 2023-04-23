using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models.Modmail;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class RequestModmailTicketHandler : IRequestHandler<RequestModmailTicketCommand>
{
    private const string RealNameButtonId = "realNameButton";
    private const string PseudonymButtonId = "pseudonymButton";
    private const string CancelButtonId = "cancelButton";
    private const string CancelMessageToken = "cancel";

    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;
    private readonly ModmailTicketPool _ticketPool;

    public RequestModmailTicketHandler(IOptions<BotOptions> options, DiscordResolver resolver, ModmailTicketPool ticketPool)
    {
        _options = options.Value;
        _resolver = resolver;
        _ticketPool = ticketPool;
    }

    public async Task Handle(RequestModmailTicketCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var channel = ctx.Channel;
        var requestMessage = request.RequestMessage;

        var member = await _resolver.ResolveGuildMember(ctx.User.Id);

        if (member == null)
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

        var existingTicket = _ticketPool.Get(ctx.User.Id);

        if (existingTicket != null)
        {
            var messageContent = "You already have an open ticket! Just type your message here and I will pass it on.";

            await member.SendMessageAsync(messageContent);

            return;
        }

        var choice = await CollectIdentityChoice(member, cancellationToken);

        if (choice == CancelButtonId)
        {
            await HandleCancellation(member);

            return;
        }

        var requesterIsAnonymous = choice == PseudonymButtonId;

        string requesterDisplayName;

        if (!requesterIsAnonymous)
        {
            requesterDisplayName = member.GetNicknameOrDisplayName();
        }
        else
        {
            requesterDisplayName = PseudonymGenerator.GetRandomPseudonym();
        }

        var stub = CreateStubModmailTicket(ctx, requesterDisplayName, requesterIsAnonymous);

        await member.SendMessageAsync($"Understood. Your message will be sent as **{requesterDisplayName}**.");

        DiscordMessage messageToRelay;

        if (requestMessage != null)
        {
            var confirmed = await CollectRequestMessageConfirmation(requestMessage, member);

            if (!confirmed)
            {
                await HandleCancellation(member, stub);

                return;
            }

            messageToRelay = requestMessage;
        }
        else
        {
            var collectedMessage = await CollectRequestMessage(member);

            if (collectedMessage == null || collectedMessage.Content.Equals(CancelMessageToken, StringComparison.InvariantCultureIgnoreCase))
            {
                await HandleCancellation(member, stub);

                return;
            }

            messageToRelay = collectedMessage;
        }

        await PostStubModmailTicket(stub, messageToRelay.Content);

        await messageToRelay.CreateSuccessReactionAsync();

        var ticketCreateMessage = """
            Thanks! A staff member will get back to you soon.
            In the meanwhile you may provide additional information if you so desire.
            """;

        await member.SendMessageAsync(ticketCreateMessage);
    }

    /// <summary>
    /// Collect a request message if the user didn't provide one in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage"/>.</returns>
    private static async Task<DiscordMessage?> CollectRequestMessage(DiscordMember member)
    {
        var messageContent = "Please type your message here and I will pass it on. If you want to cancel just type `cancel`.";

        var promptForMessageMessage = await member.SendMessageAsync(messageContent);

        var promptInteractivityResult = await promptForMessageMessage.Channel.GetNextMessageAsync(member);

        if (promptInteractivityResult.TimedOut)
            return null;

        return promptInteractivityResult.Result;
    }

    /// <summary>
    /// Collect confirmation from user if they provided a request message in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage"/>.</returns>
    private static async Task<bool> CollectRequestMessageConfirmation(DiscordMessage message, DiscordMember member)
    {
        var messageContent = $"""
            The following message will be sent to the moderators.
            > {message.Content}
            Please react with {EmojiMap.WhiteCheckmark} to confirm or {EmojiMap.RedX} to cancel.
            """;

        var promptForConfirmMessage = await member.SendMessageAsync(messageContent);

        await promptForConfirmMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        var promptInteractivityResult = await promptForConfirmMessage.WaitForReactionAsync(member);

        if (promptInteractivityResult.TimedOut)
            return false;

        var choice = promptInteractivityResult.Result.Emoji.Name;

        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        return choice == EmojiMap.WhiteCheckmark;
    }

    /// <summary>
    /// Collect choice of identity from user i.e. use real name or pseudonym.
    /// </summary>
    /// <returns>Returns true if the user chooses to be anonymous.</returns>
    private static async Task<string> CollectIdentityChoice(DiscordMember member, CancellationToken cancellationToken)
    {
        var realName = member.GetNicknameOrDisplayName();

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
            return CancelButtonId; // Assume cancelled

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, choiceInteractionResponseBuilder);

        return interactionResult.Result.Id;
    }

    private async Task HandleCancellation(DiscordMember member, ModmailTicket? stub = null)
    {
        if (stub != null) CancelStubModmailTicket(stub);

        await member.SendMessageAsync("Understood. You can always request a ticket again later.");
    }

    private ModmailTicket CreateStubModmailTicket(BaseContext ctx, string requesterDisplayName, bool requesterIsAnonymous)
    {
        var modmailTicket = new ModmailTicket
        {
            IsAnonymous = requesterIsAnonymous,
            RequesterId = ctx.User.Id,
            RequesterDisplayName = requesterDisplayName,
        };

        if (!_ticketPool.TryAdd(modmailTicket))
            throw new Exception("Failed to add stub ticket to pool as it already exists.");

        return modmailTicket;
    }

    private void CancelStubModmailTicket(ModmailTicket ticket)
    {
        _ = _ticketPool.TryRemove(ticket);
    }

    private async Task PostStubModmailTicket(ModmailTicket ticket, string requestMesage)
    {
        var modmailChannelId = _options.ModmailChannelId;

        var modmailChannel = (DiscordForumChannel)_resolver.ResolveGuild().Channels[modmailChannelId];

        var modmailPostTitle = ticket.IsAnonymous
            ? $"Ticket from anonymous user {ticket.RequesterDisplayName}"
            : $"Ticket from server member {ticket.RequesterDisplayName}";

        var relayMessageContent = $"""
            {ticket.RequesterDisplayName} says
            > {requestMesage}
            """;

        var modmailPostMessage = new DiscordMessageBuilder()
            .WithContent(relayMessageContent);

        var fpBuilder = new ForumPostBuilder()
            .WithName(modmailPostTitle)
            .WithMessage(modmailPostMessage);

        // Refresh the ticket in case it was updated while waiting for the user to type their message.
        var refreshedTicket = _ticketPool.Get(ticket.RequesterId);

        if (refreshedTicket!.TicketPost != null)
            throw new Exception("Ticket already posted");

        var forumPost = await modmailChannel.CreateForumPostAsync(fpBuilder);

        var postedTicket = refreshedTicket with
        {
            TicketPost = new ModmailTicketPost(forumPost.Channel.Id, forumPost.Message.Id),
        };

        _ = _ticketPool.TryUpdate(postedTicket);
    }
}
