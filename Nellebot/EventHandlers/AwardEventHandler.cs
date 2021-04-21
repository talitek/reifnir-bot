using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.EventHandlers
{
    public class AwardEventHandler
    {
        private readonly DiscordClient _client;
        private readonly ILogger<AwardEventHandler> _logger;
        private readonly MessageAwardQueue _awardQueue;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly BotOptions _options;

        public AwardEventHandler(
            DiscordClient client,
            ILogger<AwardEventHandler> logger,
            MessageAwardQueue awardQueue,
            IOptions<BotOptions> options,
            DiscordErrorLogger discordErrorLogger)
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
                _logger.LogError(ex, "OnMessageReactionAdded");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
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
                _logger.LogError(ex, "OnMessageReactionRemoved");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
            }
        }

        private async Task OnMessageUpdated(DiscordClient sender, MessageUpdateEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var user = eventArgs.Author;

                if (!ShouldHandleReaction(channel, user))
                    return;

                if (!IsAwardAllowedChannel(channel))
                    return;

                _awardQueue.Enqueue(new MessageAwardQueueItem(message, MessageAwardQueueAction.MessageUpdated));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageReactionAdded");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
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

                if (!IsAwardAllowedChannel(channel))
                    return;

                _awardQueue.Enqueue(new MessageAwardQueueItem(messageId, channel, MessageAwardQueueAction.MessageDeleted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageReactionAdded");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
            }
        }

        private bool IsAwardAllowedChannel(DiscordChannel channel)
        {
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

        ///// <summary>
        ///// Don't care about about private messages
        ///// Don't care about bot reactions
        ///// </summary>
        ///// <returns></returns>
        private bool ShouldHandleReaction(DiscordChannel channel, DiscordUser author)
        {
            if (channel.IsPrivate)
                return false;

            if (author.IsBot || (author.IsSystem ?? false))
                return false;

            return true;
        }
    }
}
