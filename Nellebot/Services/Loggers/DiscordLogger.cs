using Microsoft.Extensions.Options;
using Nellebot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services.Loggers
{
    public class DiscordLogger
    {
        private readonly BotOptions _options;
        private readonly DiscordResolver _discordResolver;

        public DiscordLogger(IOptions<BotOptions> options, DiscordResolver discordResolver)
        {
            _options = options.Value;
            _discordResolver = discordResolver;
        }

        public async Task LogGreetingMessage(string message)
        {
            var guildId  = _options.GuildId;
            var channelId = _options.GreetingsChannelId;

            var greetingChannel = await _discordResolver.ResolveChannel(guildId, channelId);

            if (greetingChannel != null)
            {
                await greetingChannel.SendMessageAsync(message);
            }
        }

    }
}
