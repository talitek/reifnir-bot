using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record RequestModmailTicketCommand : BaseCommand
{
    public RequestModmailTicketCommand(BaseContext ctx)
        : base(ctx) { }
}

public class RequestModmailTicketHandler : IRequestHandler<RequestModmailTicketCommand>
{
    private readonly BotOptions _options;
    private readonly DiscordResolver _resolver;

    public RequestModmailTicketHandler(IOptions<BotOptions> options, DiscordResolver resolver)
    {
        _options = options.Value;
        _resolver = resolver;
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

        if (interactionResult.Result.Id == "realNameButton")
        {
            var nickname = ctx.Member?.GetNicknameOrDisplayName() ?? ctx.User.GetFullUsername();

            responseBuilder = responseBuilder.WithContent($"Wow, **{nickname}**. You're a real chad!");
        }
        else
        {
            var nickname = PseudonymGenerator.GetRandomPseudonym();

            responseBuilder = responseBuilder.WithContent($"Wow, **{nickname}**. You're a real virgin!");
        }

        var dmChannel = introMessage.Channel;

#if DEBUG
        dmChannel = _resolver.ResolveGuild().Channels[_options.FakeDmChannelId!.Value];
#endif

        await dmChannel.SendMessageAsync(responseBuilder);
    }
}
