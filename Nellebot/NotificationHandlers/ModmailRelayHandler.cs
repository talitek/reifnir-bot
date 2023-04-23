using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Services;
using Nellebot.Utils;
using Nellebot.Workers;
using BaseContext = Nellebot.CommandHandlers.BaseContext;

namespace Nellebot.NotificationHandlers;

public class ModmailRelayHandler : INotificationHandler<MessageCreatedNotification>
{
    private const string CancelMessageToken = "cancel";

    private readonly CommandQueueChannel _commandQueue;
    private readonly ModmailTicketPool _ticketPool;
    private readonly BotOptions _botOptions;

    public ModmailRelayHandler(CommandQueueChannel commandQueue, ModmailTicketPool ticketPool, IOptions<BotOptions> botOptions)
    {
        _commandQueue = commandQueue;
        _ticketPool = ticketPool;
        _botOptions = botOptions.Value;
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
            return HandlePrivateMessage(channel, user, message, cancellationToken);
        }

        if (channel.ParentId == _botOptions.ModmailChannelId)
        {
            return HandleThreadMessage(channel, user, message, cancellationToken);
        }

        // It's a non-private message in a random-ass channel
        return Task.CompletedTask;
    }

    private Task HandlePrivateMessage(DiscordChannel channel, DiscordUser user, DiscordMessage message, CancellationToken cancellationToken)
    {
        // The message could be an interactivity response containing the token "cancel".
        // If so, disregard the message. Not the most elegant solution, but it should do.
        if (message.Content.Equals(CancelMessageToken, StringComparison.InvariantCultureIgnoreCase))
            return Task.CompletedTask;

        var userTicketInPool = _ticketPool.Get(user.Id);

        if (userTicketInPool == null)
        {
            var baseContext = new BaseContext
            {
                Channel = channel,
                User = user,
            };

            var requestTicketCommand = new RequestModmailTicketCommand(baseContext, message);

            return _commandQueue.Writer.WriteAsync(requestTicketCommand, cancellationToken).AsTask();
        }

        // User is still in the process of requesting a ticket
        if (userTicketInPool.IsStub) return Task.CompletedTask;

        var requesterMessageContext = new MessageContext
        {
            Channel = channel,
            User = user,
            Message = message,
        };

        var relayRequesterMessageCommand = new RelayRequesterMessageCommand(requesterMessageContext, userTicketInPool);

        return _commandQueue.Writer.WriteAsync(relayRequesterMessageCommand, cancellationToken).AsTask();
    }

    private Task HandleThreadMessage(DiscordChannel channel, DiscordUser user, DiscordMessage message, CancellationToken cancellationToken)
    {
        var channelTicketInPool = _ticketPool.GetTicketByChannelId(channel.Id);

        if (channelTicketInPool == null)
        {
            return message.CreateFailureReactionAsync();
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
