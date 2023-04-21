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

        var existingTicket = _ticketPool.GetTicketByUserId(ctx.User.Id);

        if (existingTicket != null)
        {
            var messageContent = "You already have an open ticket! Just type your message here and I will pass it on.";

            await member.SendMessageAsync(messageContent);

            return;
        }

        var requesterIsAnonymous = await CollectIdentityChoice(member, cancellationToken);

        string requesterDisplayName;

        if (!requesterIsAnonymous)
        {
            requesterDisplayName = member.GetNicknameOrDisplayName();
        }
        else
        {
            requesterDisplayName = PseudonymGenerator.GetRandomPseudonym();
        }

        await member.SendMessageAsync($"Understood. Your message will be sent as **{requesterDisplayName}**.");

        var stub = CreateStubModmailTicket(ctx, requesterDisplayName, requesterIsAnonymous);

        DiscordMessage messageToRelay;

        if (requestMessage != null && await CollectRequestMessageConfirmation(requestMessage, member))
        {
            messageToRelay = requestMessage;
        }
        else
        {
            messageToRelay = await CollectRequestMessage(member);
        }

        await PostStubModmailTicket(stub, messageToRelay.Content);

        await messageToRelay.CreateSuccessReactionAsync();
    }

    /// <summary>
    /// Collect a request message if the user didn't provide one in the command.
    /// </summary>
    /// <returns>The collected <see cref="DiscordMessage"/>.</returns>
    private static async Task<DiscordMessage> CollectRequestMessage(DiscordMember member)
    {
        var messageContent = "Please type your message here and I will pass it on.";

        var promptForMessageMessage = await member.SendMessageAsync(messageContent);

        var promptInteractivityResult = await promptForMessageMessage.Channel.GetNextMessageAsync(member);

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

        var choice = promptInteractivityResult.Result.Emoji.Name;

        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        await promptForConfirmMessage.DeleteOwnReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));

        return choice == EmojiMap.WhiteCheckmark;
    }

    /// <summary>
    /// Collect choice of identity from user i.e. use real name or pseudonym.
    /// </summary>
    /// <returns>Returns true if the user chooses to be anonymous.</returns>
    private static async Task<bool> CollectIdentityChoice(DiscordMember member, CancellationToken cancellationToken)
    {
        var realName = member.GetNicknameOrDisplayName();

        var introMessageContent = $"""
            Hello and welcome to Modmail! 
            Would you like to send the message as **{realName}** or do you want to use a random pseudonym?
            """;

        const string realNameButtonId = "realNameButton";
        const string pseudonymButtonId = "pseudonymButton";

        var realNameButton = new DiscordButtonComponent(ButtonStyle.Primary, realNameButtonId, realName);
        var pseudonymButton = new DiscordButtonComponent(ButtonStyle.Primary, pseudonymButtonId, "Random pseudonym");

        var introMessageBuilder = new DiscordMessageBuilder()
            .WithContent(introMessageContent)
            .AddComponents(realNameButton, pseudonymButton);

        var introMessage = await member.SendMessageAsync(introMessageBuilder);

        var interactionResult = await introMessage.WaitForButtonAsync(cancellationToken);

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, choiceInteractionResponseBuilder);

        bool requesterIsAnonymous = interactionResult.Result.Id == pseudonymButtonId;

        return requesterIsAnonymous;
    }

    private ModmailTicket CreateStubModmailTicket(BaseContext ctx, string requesterDisplayName, bool requesterIsAnonymous)
    {
        var modmailTicket = new ModmailTicket
        {
            Id = Guid.NewGuid(),
            IsAnonymous = requesterIsAnonymous,
            RequesterId = ctx.User.Id,
            RequesterDisplayName = requesterDisplayName,
        };

        _ = _ticketPool.TryAdd(modmailTicket);

        return modmailTicket;
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

        var forumPost = await modmailChannel.CreateForumPostAsync(fpBuilder);

        var postedTicket = ticket with
        {
            TicketPost = new ModmailTicketPost(forumPost.Channel.Id, forumPost.Message.Id),
        };

        _ticketPool.AddOrUpdate(postedTicket);
    }
}
