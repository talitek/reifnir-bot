using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Helpers;
using Nellebot.Workers;

namespace Nellebot.NotificationHandlers;

public class AwardsHandler : INotificationHandler<MessageReactionAddedNotification>,
    INotificationHandler<MessageReactionRemovedNotification>,
    INotificationHandler<MessageUpdatedNotification>,
    INotificationHandler<MessageDeletedNotification>
{
    private readonly MessageAwardQueueChannel _awardQueue;
    private readonly ILogger<AwardsHandler> _logger;
    private readonly BotOptions _options;

    public AwardsHandler(
        ILogger<AwardsHandler> logger,
        MessageAwardQueueChannel awardQueue,
        IOptions<BotOptions> options)
    {
        _logger = logger;
        _awardQueue = awardQueue;
        _options = options.Value;
    }

    public async Task Handle(MessageReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        var eventArgs = notification.EventArgs;
        var channel = eventArgs.Channel;
        var message = eventArgs.Message;
        var user = eventArgs.User;
        var emoji = eventArgs.Emoji;

        if (!ShouldHandleReaction(channel, user)) return;

        var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

        if (!IsAwardAllowedChannel(channel)) return;

        if (!isAwardEmoji) return;

        await _awardQueue.Writer.WriteAsync(
            new MessageAwardItem(message, MessageAwardQueueAction.ReactionChanged),
            cancellationToken);
    }

    public async Task Handle(MessageReactionRemovedNotification notification, CancellationToken cancellationToken)
    {
        var eventArgs = notification.EventArgs;
        var channel = eventArgs.Channel;
        var message = eventArgs.Message;
        var emoji = eventArgs.Emoji;

        if (channel.IsPrivate) return;

        var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

        if (!IsAwardAllowedChannel(channel)) return;

        if (!isAwardEmoji) return;

        await _awardQueue.Writer.WriteAsync(
            new MessageAwardItem(message, MessageAwardQueueAction.ReactionChanged),
            cancellationToken);
    }

    public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        var eventArgs = notification.EventArgs;
        var channel = eventArgs.Channel;
        var message = eventArgs.Message;
        var user = eventArgs.Author;

        if (message == null) throw new Exception($"{nameof(eventArgs.Message)} is null");

        if (!ShouldHandleReaction(channel, user)) return;

        if (!IsAwardAllowedChannel(channel)) return;

        await _awardQueue.Writer.WriteAsync(
            new MessageAwardItem(message, MessageAwardQueueAction.MessageUpdated),
            cancellationToken);
    }

    public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        var eventArgs = notification.EventArgs;
        var channel = eventArgs.Channel;
        var messageId = eventArgs.Message.Id;

        if (channel.IsPrivate) return;

        if (IsAwardAllowedChannel(channel))
        {
            await _awardQueue.Writer.WriteAsync(
                new MessageAwardItem(
                    messageId,
                    channel,
                    MessageAwardQueueAction.MessageDeleted),
                cancellationToken);

            return;
        }

        if (IsAwardChannel(channel))
        {
            await _awardQueue.Writer.WriteAsync(
                new MessageAwardItem(
                    messageId,
                    channel,
                    MessageAwardQueueAction.AwardDeleted),
                cancellationToken);
        }
    }

    private bool IsAwardAllowedChannel(DiscordChannel channel)
    {
        if (channel.IsThread) channel = channel.Parent;

        var allowedGroupIds = _options.AwardVoteGroupIds;

        if (allowedGroupIds.Length == 0)
        {
            _logger.LogDebug($"{nameof(_options.AwardVoteGroupIds)} is empty");
            return false;
        }

        var isAllowedChannel = allowedGroupIds.ToList().Contains(channel.ParentId!.Value);

        return isAllowedChannel;
    }

    private bool IsAwardChannel(DiscordChannel channel)
    {
        var awardChannelId = _options.AwardChannelId;

        return channel.Id == awardChannelId;
    }

    /// <summary>
    ///     Don't care about about private messages
    ///     Don't care about bot reactions.
    /// </summary>
    private bool ShouldHandleReaction(DiscordChannel channel, DiscordUser? author)
    {
        // This seems to happen because of the newly introduced Threads feature
        if (author is null) return false;

        if (author.IsBot || (author.IsSystem ?? false)) return false;

        return !channel.IsPrivate;
    }
}
