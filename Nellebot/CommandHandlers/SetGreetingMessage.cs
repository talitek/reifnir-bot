using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Services;

namespace Nellebot.CommandHandlers;

public class SetGreetingMessageRequest : CommandRequest
{
    public string GreetingMessage { get; set; }

    public SetGreetingMessageRequest(CommandContext ctx, string greetingMessage)
        : base(ctx)
    {
        GreetingMessage = greetingMessage;
    }
}

public class SetGreetingMessageHandler : IRequestHandler<SetGreetingMessageRequest>
{
    private readonly BotSettingsService _botSettingsService;

    public SetGreetingMessageHandler(BotSettingsService botSettingsService)
    {
        _botSettingsService = botSettingsService;
    }

    public async Task Handle(SetGreetingMessageRequest request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var message = request.GreetingMessage;

        await _botSettingsService.SetGreetingMessage(message);

        var previewMemberMention = ctx.Member?.Mention ?? string.Empty;

        var messagePreview = await _botSettingsService.GetGreetingsMessage(previewMemberMention);

        var sb = new StringBuilder("Greeting mesage updated successfully. Here's a preview:");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine(messagePreview);

        await ctx.RespondAsync(sb.ToString());
    }
}
