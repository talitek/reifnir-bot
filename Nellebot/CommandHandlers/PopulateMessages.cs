using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Services;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers;

public class PopulateMessagesRequest : CommandRequest
{
    public PopulateMessagesRequest(CommandContext ctx) : base(ctx) { }
}

public class PopulateMessages : AsyncRequestHandler<PopulateMessagesRequest>
{
    private readonly MessageRefsService _messageRefsService;

    public PopulateMessages(MessageRefsService messageRefsService)
    {
        _messageRefsService = messageRefsService;
    }

    protected override async Task Handle(PopulateMessagesRequest request, CancellationToken cancellationToken)
    {
        var outputChannel = request.Ctx.Channel;

        var createdCount = await _messageRefsService.PopulateMessageRefsInit(outputChannel);

        if (createdCount != 0)
            await outputChannel.SendMessageAsync($"Populated a total of {createdCount} message refs");
        else
            await outputChannel.SendMessageAsync("No messages to populate");
    }
}
