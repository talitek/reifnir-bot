using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Helpers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Nellebot.Workers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nellebot.EventHandlers
{
    public class AwardEventHandler
    {
        private readonly DiscordClient _client;
        private readonly ILogger<AwardEventHandler> _logger;
        private readonly MessageAwardQueue _awardQueue;
        private readonly IDiscordErrorLogger _discordErrorLogger;
        private readonly BotOptions _options;

        public AwardEventHandler(
            DiscordClient client,
            ILogger<AwardEventHandler> logger,
            MessageAwardQueue awardQueue,
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

        private async Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
        {
            var channel = eventArgs.Channel;
            var message = eventArgs.Message;
            var user = eventArgs.User;
            var emoji = eventArgs.Emoji;

            try
            {
                if (!ShouldHandleReaction(channel, user))
                    return;

                var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

                if (!IsAwardAllowedChannel(channel))
                    return;

                if (isAwardEmoji)
                {
                    _awardQueue.Enqueue(new MessageAwardQueueItem(message, MessageAwardQueueAction.ReactionChanged));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageReactionAdded));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageReactionAdded),
                    User = eventArgs.User,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild
                };

                await _discordErrorLogger.LogEventError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageReactionRemoved(DiscordClient sender, MessageReactionRemoveEventArgs eventArgs)
        {
            var channel = eventArgs.Channel;
            var message = eventArgs.Message;
            var emoji = eventArgs.Emoji;

            try
            {
                if (channel.IsPrivate)
                    return;

                var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

                if (!IsAwardAllowedChannel(channel))
                    return;

                if (isAwardEmoji)
                {
                    _awardQueue.Enqueue(new MessageAwardQueueItem(message, MessageAwardQueueAction.ReactionChanged));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageReactionRemoved));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageReactionRemoved),
                    User = eventArgs.User,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild
                };

                await _discordErrorLogger.LogEventError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var user = eventArgs.Author;

                if (message == null)
                    throw new Exception($"{nameof(eventArgs.Message)} is null");

                if (!ShouldHandleReaction(channel, user))
                    return;

                if (!IsAwardAllowedChannel(channel))
                    return;

                _awardQueue.Enqueue(new MessageAwardQueueItem(message, MessageAwardQueueAction.MessageUpdated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageUpdated));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageUpdated),
                    User = eventArgs.Author,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild,
                    Message = eventArgs.Message
                };

                await _discordErrorLogger.LogEventError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageDeleted(DiscordClient sender, MessageDeleteEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var messageId = eventArgs.Message.Id;

                if (channel.IsPrivate)
                    return;

                if (IsAwardAllowedChannel(channel))
                {
                    _awardQueue.Enqueue(new MessageAwardQueueItem(messageId, channel, MessageAwardQueueAction.MessageDeleted));
                }
                else if (IsAwardChannel(channel))
                {
                    _awardQueue.Enqueue(new MessageAwardQueueItem(messageId, channel, MessageAwardQueueAction.AwardDeleted));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageDeleted));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageDeleted),
                    User = null,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild
                };

                await _discordErrorLogger.LogEventError(eventContextError, ex.ToString());
            }
        }

        private bool IsAwardAllowedChannel(DiscordChannel channel)
        {
            if (channel.IsThread) channel = channel.Parent;

            var allowedGroupIds = _options.AwardVoteGroupIds;

            if (allowedGroupIds == null || allowedGroupIds.Length == 0)
            {
                _logger.LogDebug($"{nameof(_options.AwardVoteGroupIds)} is empty");
                return false;
            }

            var isAllowedChannel = allowedGroupIds.ToList().Contains(channel.ParentId!.Value);

            if (!isAllowedChannel)
                return false;

            return true;
        }

        private bool IsAwardChannel(DiscordChannel channel)
        {
            var awardChannelId = _options.AwardChannelId;

            return channel.Id == awardChannelId;
        }

        ///// <summary>
        ///// Don't care about about private messages
        ///// Don't care about bot reactions
        ///// </summary>
        ///// <returns></returns>
        private bool ShouldHandleReaction(DiscordChannel channel, DiscordUser author)
        {
            // This seems to happen because of the newly introduced Threads feature
            if (author == null)
                return false;

            if (author.IsBot || (author.IsSystem ?? false))
                return false;

            if (channel.IsPrivate)
                return false;

            return true;
        }
    }
}
