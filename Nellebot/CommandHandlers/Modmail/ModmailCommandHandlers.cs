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

public class ModmailCommandHandlers : IRequestHandler<RequestModmailTicketCommand>,
                                            IRequestHandler<RelayRequesterMessageCommand>,
                                            IRequestHandler<RelayModeratorMessageCommand>
{
    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;
    private readonly ModmailTicketPool _ticketPool;

    public ModmailCommandHandlers(IOptions<BotOptions> options, DiscordResolver resolver, ModmailTicketPool ticketPool)
    {
        _options = options.Value;
        _resolver = resolver;
        _ticketPool = ticketPool;
    }

    public async Task Handle(RequestModmailTicketCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;

        var introMessageContent = """
            Hello and welcome to Modmail! 
            Do you want to be a Chad and use your real (discord) name or be a Virgin and use a pseudonym?
            """;

        var realNameButton = new DiscordButtonComponent(ButtonStyle.Primary, "realNameButton", "Chad");
        var pseudonymButton = new DiscordButtonComponent(ButtonStyle.Primary, "pseudonymButton", "Virgin");

        var introMessageBuilder = new DiscordMessageBuilder()
            .WithContent(introMessageContent)
            .AddComponents(realNameButton, pseudonymButton);

#if DEBUG
        var introMessage = await _resolver.ResolveGuild().Channels[_options.FakeDmChannelId!.Value].SendMessageAsync(introMessageBuilder);
#else
        var introMessage = await ctx.Member.SendMessageAsync(introMessageBuilder);
#endif

        var interactionResult = await introMessage.WaitForButtonAsync(cancellationToken);

        var choiceInteractionResponseBuilder = new DiscordInteractionResponseBuilder(introMessageBuilder);
        choiceInteractionResponseBuilder.ClearComponents();

        await interactionResult.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, choiceInteractionResponseBuilder);

        var responseBuilder = new DiscordMessageBuilder();
        string requesterDisplayName;

        bool requesterIsAnonymous = interactionResult.Result.Id == "pseudonymButton";

        if (!requesterIsAnonymous)
        {
            requesterDisplayName = ctx.Member?.GetNicknameOrDisplayName() ?? ctx.User.GetFullUsername();

            responseBuilder = responseBuilder.WithContent($"Wow, **{requesterDisplayName}**. You're a real chad!");
        }
        else
        {
            requesterDisplayName = PseudonymGenerator.GetRandomPseudonym();

            responseBuilder = responseBuilder.WithContent($"Wow, **{requesterDisplayName}**. You're a real virgin!");
        }

        var dmChannel = introMessage.Channel;

#if DEBUG
        dmChannel = _resolver.ResolveGuild().Channels[_options.FakeDmChannelId!.Value];
#endif

        await dmChannel.SendMessageAsync(responseBuilder);

        var modmailChannelId = _options.ModmailChannelId;

        var modmailChannel = _resolver.ResolveGuild().Channels[modmailChannelId];

        var modmailMessageBuilder = new DiscordMessageBuilder()
            .WithContent($"Ticket from {requesterDisplayName}**");

        var modmailMessage = await modmailChannel.SendMessageAsync(modmailMessageBuilder);

        var modmailTicket = new ModmailTicket
        {
            Id = Guid.NewGuid(),
            IsAnonymous = requesterIsAnonymous,
            RequesterId = ctx.User.Id,
            RequesterDisplayName = requesterDisplayName,
            ThreadId = modmailMessage.Id,
        };

        _ = _ticketPool.Add(modmailTicket);
    }

    public Task Handle(RelayRequesterMessageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Handle(RelayModeratorMessageCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
