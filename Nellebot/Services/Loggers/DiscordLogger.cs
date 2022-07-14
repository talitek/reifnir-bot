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
            await LogMessageCore(message, _options.GreetingsChannelId);
        }

        public async Task LogActivityMessage(string message)
        {
            await LogMessageCore(message, _options.ActivityLogChannelId);
        }

        public async Task LogExtendedActivityMessage(string message)
        {
            await LogMessageCore(message, _options.ExtendedActivityLogChannelId);
        }

        private async Task LogMessageCore(string message, ulong channelId)
        {
            var guildId = _options.GuildId;

            var channel = await _discordResolver.ResolveChannel(guildId, channelId);

            if (channel == null) return;

            await channel.SendMessageAsync(message);
        }

    }
}
