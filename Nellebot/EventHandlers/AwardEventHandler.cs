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
        private readonly BotOptions _options;

        public AwardEventHandler(
            DiscordClient client,
            ILogger<AwardEventHandler> logger,
            MessageAwardQueue awardQueue,
            IOptions<BotOptions> options)
        {
            _client = client;
            _logger = logger;
            _awardQueue = awardQueue;
            _options = options.Value;
        }

        public void RegisterHandlers()
        {
            _client.MessageReactionAdded += OnMessageReactionAdded;
        }

        private Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var user = eventArgs.User;
                var emoji = eventArgs.Emoji;

                if (!ShouldHandleReaction(channel, user))
                    return Task.CompletedTask;

                var isAwardEmoji = emoji.Name == EmojiMap.Cookie;

                if(isAwardEmoji)
                {
                    _awardQueue.Enqueue(new MessageAwardQueueItem(message));
                }                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageReactionAdded");
            }

            return Task.CompletedTask;
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
