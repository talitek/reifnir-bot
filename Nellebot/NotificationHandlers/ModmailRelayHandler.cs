using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using MediatR;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Services;
using Nellebot.Workers;
using BaseContext = Nellebot.CommandHandlers.BaseContext;

namespace Nellebot.NotificationHandlers;

public class ModmailRelayHandler : INotificationHandler<MessageCreatedNotification>
{
    private readonly CommandQueueChannel _commandQueue;
    private readonly ModmailTicketPool _ticketPool;

    public ModmailRelayHandler(CommandQueueChannel commandQueue, ModmailTicketPool ticketPool)
    {
        _commandQueue = commandQueue;
        _ticketPool = ticketPool;
    }

    public Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        var args = notification.EventArgs;

        var channel = args.Channel;
        var user = args.Author;
        var message = args.Message;

        if (user.IsBot) return Task.CompletedTask;

        if (channel.IsPrivate)
        {
            var userTicketInPool = _ticketPool.GetTicketByUserId(user.Id);

            // TODO Check if a ticket is already in the process of being requested
            if (userTicketInPool == null)
            {
                var baseContext = new BaseContext
                {
                    Channel = channel,
                    User = user,
                };

                var requestTicketCommand = new RequestModmailTicketCommand(baseContext, message.Content);

                return _commandQueue.Writer.WriteAsync(requestTicketCommand, cancellationToken).AsTask();
            }

            var requesterMessageContext = new MessageContext
            {
                Channel = channel,
                User = user,
                Message = message,
            };

            var relayRequesterMessageCommand = new RelayRequesterMessageCommand(requesterMessageContext, userTicketInPool);

            return _commandQueue.Writer.WriteAsync(relayRequesterMessageCommand, cancellationToken).AsTask();
        }

        if (channel.Type != ChannelType.PublicThread)
        {
            // return
        }

        var channelTicketInPool = _ticketPool.GetTicketByChannelId(channel.Id);

        if (channelTicketInPool == null)
        {
            // It's a non-private message in a random-ass thread
            return Task.CompletedTask;
        }

        var moderatorMessageContext = new MessageContext
        {
            Channel = channel,
            User = user,
            Message = message,
        };

        var relayModeratorMessageCommand = new RelayModeratorMessageCommand(moderatorMessageContext, channelTicketInPool);

        return _commandQueue.Writer.WriteAsync(relayModeratorMessageCommand, cancellationToken).AsTask();
    }
}
