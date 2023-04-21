using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.Models.Modmail;
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

        var responseBuilder = new DiscordMessageBuilder();
        string requesterDisplayName;

        if (!requesterIsAnonymous)
        {
            requesterDisplayName = member.GetNicknameOrDisplayName();

            responseBuilder = responseBuilder.WithContent($"Wow, **{requesterDisplayName}**. You're a real chad!");
        }
        else
        {
            requesterDisplayName = PseudonymGenerator.GetRandomPseudonym();

            responseBuilder = responseBuilder.WithContent($"Wow, **{requesterDisplayName}**. You're a real virgin!");
        }

        await member.SendMessageAsync(responseBuilder);

        await CreateTicketForumPost(ctx, requesterDisplayName, requesterIsAnonymous);
    }

    /// <summary>
    /// Collect choice of identity from user i.e. use real name or pseudonym.
    /// </summary>
    /// <returns>Returns true if the user chooses to be anonymous.</returns>
    private static async Task<bool> CollectIdentityChoice(DiscordMember member, CancellationToken cancellationToken)
    {
        var introMessageContent = """
            Hello and welcome to Modmail! 
            Do you want to be a Chad and use your real (discord) name or be a Virgin and use a pseudonym?
            """;

        var realNameButton = new DiscordButtonComponent(ButtonStyle.Primary, "realNameButton", "Chad");
        var pseudonymButton = new DiscordButtonComponent(ButtonStyle.Primary, "pseudonymButton", "Virgin");

        var introMessageBuilder = new DiscordMessageBuilder()
            .WithContent(introMessageContent)
            .AddComponents(realNameButton, pseudonymButton);

        var introMessage = await member.SendMessageAsync(introMessageBuilder);

        var interactionResult = await introMessage.WaitForButtonAsync(cancellationToken);

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, choiceInteractionResponseBuilder);

        bool requesterIsAnonymous = interactionResult.Result.Id == "pseudonymButton";

        return requesterIsAnonymous;
    }

    private async Task CreateTicketForumPost(BaseContext ctx, string requesterDisplayName, bool requesterIsAnonymous)
    {
        var modmailChannelId = _options.ModmailChannelId;

        var modmailChannel = (DiscordForumChannel)_resolver.ResolveGuild().Channels[modmailChannelId];

        var modmailPostTitle = requesterIsAnonymous
            ? $"Ticket from anonymous user {requesterDisplayName}"
            : $"Ticket from server member {requesterDisplayName}";

        var modmailPostMessage = new DiscordMessageBuilder().WithContent($"**{requesterDisplayName}** whining about something");

        var fpBuilder = new ForumPostBuilder()
            .WithName(modmailPostTitle)
            .WithMessage(modmailPostMessage);

        var forumPostChannel = await modmailChannel.CreateForumPostAsync(fpBuilder);

        var modmailTicket = new ModmailTicket
        {
            Id = Guid.NewGuid(),
            IsAnonymous = requesterIsAnonymous,
            RequesterId = ctx.User.Id,
            RequesterDisplayName = requesterDisplayName,
            ForumPostChannelId = forumPostChannel.Channel.Id,
            ForumPostMessageId = forumPostChannel.Message.Id,
        };

        _ = _ticketPool.TryAdd(modmailTicket);
    }
}
