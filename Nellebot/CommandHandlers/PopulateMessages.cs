using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Services;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers;

public class PopulateMessagesRequest : CommandRequest
{
    public PopulateMessagesRequest(CommandContext ctx) : base(ctx) { }
}

public class PopulateMessagesHandler : AsyncRequestHandler<PopulateMessagesRequest>
{
    private readonly MessageRefsService _messageRefsService;

    public PopulateMessagesHandler(MessageRefsService messageRefsService)
    {
        _messageRefsService = messageRefsService;
    }

    protected override async Task Handle(PopulateMessagesRequest request, CancellationToken cancellationToken)
    {
        var guild = request.Ctx.Guild;
        var channel = request.Ctx.Channel;        

        var createdMessages = await _messageRefsService.PopulateMessageRefsInit(guild);

        if(createdMessages.Count == 0)
        {
            await channel.SendMessageAsync("No messages to populate");
            return;
        }

        var messagesInChannel = createdMessages.GroupBy(x => x.Channel.Name).ToList();

        foreach (var messageGroup in messagesInChannel)
        {
            await channel.SendMessageAsync($"Populated {messageGroup.Count()} message refs in {messageGroup.Key}");
        }
    }
}
