using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Attributes
{
    /// <summary>
    /// Reject commands coming from DM or from other guild (if it exists)
    /// </summary>
    public class BaseCommandCheck : CheckBaseAttribute
    {
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var botOptionsObj = ctx.Services.GetService(typeof(IOptions<BotOptions>));

            if (botOptionsObj == null)
            {
                var error = "Could not fetch AuthorizationService";

                var discordErrorLoggerObj = ctx.Services.GetService(typeof(DiscordErrorLogger));

                if (discordErrorLoggerObj == null)
                {
                    throw new Exception("Could not fetch DiscordErrorLogger");
                }

                var discordErrorLogger = (DiscordErrorLogger)discordErrorLoggerObj;

                await discordErrorLogger.LogDiscordError(error);

                return false;
            }

            var botOptions = ((IOptions<BotOptions>)botOptionsObj).Value;

            var guildId = botOptions.NelleGuildId;

            var channel = ctx.Channel;

            if (IsPrivateMessageChannel(channel))
                return false;

            if (!IsGuildChannel(channel, guildId))
                return false;

            return true;
        }

        private bool IsGuildChannel(DiscordChannel channel, ulong botGuildId)
        {
            return channel.GuildId == botGuildId;
        }

        private bool IsPrivateMessageChannel(DiscordChannel channel)
        {
            return channel.IsPrivate;
        }
    }
}
