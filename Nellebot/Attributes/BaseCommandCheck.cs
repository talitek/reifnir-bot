using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;

namespace Nellebot.Attributes;

/// <summary>
///     Reject commands coming from DM or from other guild (if it exists).
/// </summary>
public class BaseCommandCheck : CheckBaseAttribute
{
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        var botOptionsObj = ctx.Services.GetService(typeof(IOptions<BotOptions>));

        if (botOptionsObj == null)
        {
            var error = $"Could not fetch {typeof(IOptions<BotOptions>).Name}";

            var discordErrorLoggerObj = ctx.Services.GetService(typeof(IDiscordErrorLogger));

            if (discordErrorLoggerObj == null)
            {
                throw new Exception($"Could not fetch {typeof(IDiscordErrorLogger).Name}");
            }

            var discordErrorLogger = (IDiscordErrorLogger)discordErrorLoggerObj;

            discordErrorLogger.LogError("WTF", error);

            return Task.FromResult(false);
        }

        var botOptions = ((IOptions<BotOptions>)botOptionsObj).Value;

        var guildId = botOptions.GuildId;

        var channel = ctx.Channel;

        if (IsPrivateMessageChannel(channel)) return Task.FromResult(false);

        return !IsGuildChannel(channel, guildId) ? Task.FromResult(false) : Task.FromResult(true);
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
