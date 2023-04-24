using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers;
using Nellebot.Helpers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.EventHandlers;

public class AwardEventHandler
{
    private readonly DiscordClient _client;
    private readonly ILogger<AwardEventHandler> _logger;
    private readonly MessageAwardQueueChannel _awardQueue;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly BotOptions _options;

    public AwardEventHandler(
        DiscordClient client,
        ILogger<AwardEventHandler> logger,
        MessageAwardQueueChannel awardQueue,
        IOptions<BotOptions> options,
        IDiscordErrorLogger discordErrorLogger)
    {
        _client = client;
        _logger = logger;
        _awardQueue = awardQueue;
        _discordErrorLogger = discordErrorLogger;
        _options = options.Value;
    }

    public void RegisterHandlers()
    {
        _client.MessageReactionAdded += OnMessageReactionAdded;
        _client.MessageReactionRemoved += OnMessageReactionRemoved;

        _client.MessageUpdated += OnMessageUpdated;
        _client.MessageDeleted += OnMessageDeleted;
    }

    private Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage message = eventArgs.Message;
        DiscordUser user = eventArgs.User;
        DiscordEmoji emoji = eventArgs.Emoji;

        try
        {
            if (!ShouldHandleReaction(channel, user))
            {
                return Task.CompletedTask;
            }

            bool isAwardEmoji = emoji.Name == EmojiMap.Cookie;

            if (!IsAwardAllowedChannel(channel))
            {
                return Task.CompletedTask;
            }

            return !isAwardEmoji
                ? Task.CompletedTask
                : _awardQueue.Writer.WriteAsync(new MessageAwardItem(message, MessageAwardQueueAction.ReactionChanged)).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageReactionAdded));

            var eventContextError = new EventContext()
            {
                EventName = nameof(OnMessageReactionAdded),
                User = eventArgs.User,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
            };

            _discordErrorLogger.LogEventError(eventContextError, ex.ToString());

            return Task.CompletedTask;
        }
    }

    private Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs eventArgs)
    {
        DiscordChannel channel = eventArgs.Channel;
        DiscordMessage message = eventArgs.Message;
        DiscordEmoji emoji = eventArgs.Emoji;

        try
        {
            if (channel.IsPrivate)
            {
                return Task.CompletedTask;
            }

            bool isAwardEmoji = emoji.Name == EmojiMap.Cookie;

            if (!IsAwardAllowedChannel(channel))
            {
                return Task.CompletedTask;
            }

            return !isAwardEmoji
                ? Task.CompletedTask
                : _awardQueue.Writer.WriteAsync(new MessageAwardItem(message, MessageAwardQueueAction.ReactionChanged)).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageReactionRemoved));

            var eventContextError = new EventContext()
            {
                EventName = nameof(OnMessageReactionRemoved),
                User = eventArgs.User,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
            };

            _discordErrorLogger.LogEventError(eventContextError, ex.ToString());

            return Task.CompletedTask;
        }
    }

    private Task OnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs eventArgs)
    {
        try
        {
            DiscordChannel channel = eventArgs.Channel;
            DiscordMessage message = eventArgs.Message;
            DiscordUser user = eventArgs.Author;

            if (message == null)
            {
                throw new Exception($"{nameof(eventArgs.Message)} is null");
            }

            if (!ShouldHandleReaction(channel, user))
            {
                return Task.CompletedTask;
            }

            return !IsAwardAllowedChannel(channel)
                ? Task.CompletedTask
                : _awardQueue.Writer.WriteAsync(new MessageAwardItem(message, MessageAwardQueueAction.MessageUpdated)).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageUpdated));

            var eventContextError = new EventContext()
            {
                EventName = nameof(OnMessageUpdated),
                User = eventArgs.Author,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Message = eventArgs.Message,
            };

            _discordErrorLogger.LogEventError(eventContextError, ex.ToString());

            return Task.CompletedTask;
        }
    }

    private Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs eventArgs)
    {
        try
        {
            DiscordChannel channel = eventArgs.Channel;
            ulong messageId = eventArgs.Message.Id;

            if (channel.IsPrivate)
            {
                return Task.CompletedTask;
            }

            if (IsAwardAllowedChannel(channel))
            {
                return _awardQueue.Writer.WriteAsync(new MessageAwardItem(messageId, channel, MessageAwardQueueAction.MessageDeleted)).AsTask();
            }
            else if (IsAwardChannel(channel))
            {
                return _awardQueue.Writer.WriteAsync(new MessageAwardItem(messageId, channel, MessageAwardQueueAction.AwardDeleted)).AsTask();
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageDeleted));

            var eventContextError = new EventContext()
            {
                EventName = nameof(OnMessageDeleted),
                User = null,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
            };

            _discordErrorLogger.LogEventError(eventContextError, ex.ToString());

            return Task.CompletedTask;
        }
    }

    private bool IsAwardAllowedChannel(DiscordChannel channel)
    {
        if (channel.IsThread)
        {
            channel = channel.Parent;
        }

        ulong[] allowedGroupIds = _options.AwardVoteGroupIds;

        if (allowedGroupIds.Length == 0)
        {
            _logger.LogDebug($"{nameof(_options.AwardVoteGroupIds)} is empty");
            return false;
        }

        bool isAllowedChannel = allowedGroupIds.ToList().Contains(channel.ParentId!.Value);

        return isAllowedChannel;
    }

    private bool IsAwardChannel(DiscordChannel channel)
    {
        ulong awardChannelId = _options.AwardChannelId;

        return channel.Id == awardChannelId;
    }

    /// <summary>
    /// Don't care about about private messages
    /// Don't care about bot reactions.
    /// </summary>
    private bool ShouldHandleReaction(DiscordChannel channel, DiscordUser author)
    {
        // This seems to happen because of the newly introduced Threads feature
        if (author == null)
        {
            return false;
        }

        if (author.IsBot || (author.IsSystem ?? false))
        {
            return false;
        }

        return !channel.IsPrivate;
    }
}
