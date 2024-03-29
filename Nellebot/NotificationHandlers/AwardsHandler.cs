using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class AwardsHandler : INotificationHandler<MessageReactionAddedNotification>,
    INotificationHandler<MessageReactionRemovedNotification>,
    INotificationHandler<MessageUpdatedNotification>,
    INotificationHandler<MessageDeletedNotification>
{
    private readonly AwardMessageService _awardMessageService;
    private readonly ILogger<AwardsHandler> _logger;
    private readonly BotOptions _options;

    public AwardsHandler(
        ILogger<AwardsHandler> logger,
        IOptions<BotOptions> options,
        AwardMessageService awardMessageService)
    {
        _logger = logger;
        _awardMessageService = awardMessageService;
        _options = options.Value;
    }

    public async Task Handle(MessageReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        MessageReactionAddEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage message = eventArgs.Message;
        DiscordUser user = eventArgs.User;
        DiscordEmoji emoji = eventArgs.Emoji;

        if (!ShouldHandleReaction(channel, user)) return;

        var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

        if (!IsAwardAllowedChannel(channel)) return;

        if (!isAwardEmoji) return;

        await _awardMessageService.HandleAwardChange(message);
    }

    public async Task Handle(MessageReactionRemovedNotification notification, CancellationToken cancellationToken)
    {
        MessageReactionRemoveEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage message = eventArgs.Message;
        DiscordEmoji emoji = eventArgs.Emoji;

        if (channel.IsPrivate) return;

        var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

        if (!IsAwardAllowedChannel(channel)) return;

        if (!isAwardEmoji) return;

        await _awardMessageService.HandleAwardChange(message);
    }

    public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        MessageUpdateEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage? message = eventArgs.Message;
        DiscordUser user = eventArgs.Author;

        if (message == null) throw new Exception($"{nameof(eventArgs.Message)} is null");

        if (!ShouldHandleReaction(channel, user)) return;

        if (!IsAwardAllowedChannel(channel)) return;

        await _awardMessageService.HandleAwardMessageUpdated(message);
    }

    public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessageDeleteEventArgs eventArgs = notification.EventArgs;
        DiscordChannel channel = eventArgs.Channel;
        var messageId = eventArgs.Message.Id;

        if (channel.IsPrivate) return;

        if (IsAwardAllowedChannel(channel))
        {
            await _awardMessageService.HandleAwardMessageDeleted(messageId);
        }
        else if (IsAwardChannel(channel))
        {
            await _awardMessageService.HandleAwardedMessageDeleted(messageId);
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
