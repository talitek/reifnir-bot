using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers;
using Nellebot.Helpers;
using Nellebot.Workers;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers;

public class ModmailRelayHandler : INotificationHandler<MessageCreatedNotification>
{
    private readonly BotOptions _options;
    private readonly CommandQueueChannel _channel;

    public ModmailRelayHandler(IOptions<BotOptions> options, CommandQueueChannel channel)
    {
        _options = options.Value;
        _channel = channel;
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

        //// TODO: check for open tickets

        var baseContext = new BaseContext
        {
            Channel = channel,
            User = user,
        };

        return _channel.Writer.WriteAsync(new RequestModmailTicketCommand(baseContext), cancellationToken).AsTask();
    }
}
