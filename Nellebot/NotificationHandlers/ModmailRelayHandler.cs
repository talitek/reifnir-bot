using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Services;
using Nellebot.Workers;

namespace Nellebot.NotificationHandlers;

public class ModmailRelayHandler : INotificationHandler<MessageCreatedNotification>
{
    private readonly BotOptions _options;
    private readonly CommandQueueChannel _commandQueue;
    private readonly ModmailTicketPool _ticketPool;

    public ModmailRelayHandler(IOptions<BotOptions> options, CommandQueueChannel commandQueue, ModmailTicketPool ticketPool)
    {
        _options = options.Value;
        _commandQueue = commandQueue;
        _ticketPool = ticketPool;
    }

    public Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        var args = notification.EventArgs;

        var channel = args.Channel;
        var user = args.Author;

        if (user.IsBot) return Task.CompletedTask;

#if DEBUG
        if (channel.Id != _options.FakeDmChannelId) return Task.CompletedTask;
#else
        if (!channel.IsPrivate) return;
#endif

        var ticketInPool = _ticketPool.GetTicketForUser(user.Id);

        if (ticketInPool == null)
        {
            var baseContext = new BaseContext
            {
                Channel = channel,
                User = user,
            };

            return _commandQueue.Writer.WriteAsync(new RequestModmailTicketCommand(baseContext), cancellationToken).AsTask();
        }

        return channel.SendMessageAsync("Watchu want?");
    }
}
