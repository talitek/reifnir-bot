using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Nellebot.Utils;

namespace Nellebot.Services.Loggers
{
    public class DiscordErrorLogger : IDiscordErrorLogger
    {
        private readonly DiscordClient _client;
        private readonly BotOptions _options;

        public DiscordErrorLogger(DiscordClient client, IOptions<BotOptions> options)
        {
            _client = client;
            _options = options.Value;
        }

        public Task LogCommandError(CommandContext ctx, string errorMessage)
        {
            var user = $"{ctx.User.Username}#{ctx.User.Discriminator}";
            var channelName = ctx.Channel.Name;
            var guildName = ctx.Guild.Name;
            var command = EscapeTicks(ctx.Message.Content);

            var contextMessage = $"`{command}` by `{user}` in `{channelName}`(`{guildName}`)";
            var escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

            var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

            return LogError("Failed command", fullErrorMessage);
        }

        public Task LogEventError(EventContext ctx, string errorMessage)
        {
            var user = ctx.User != null ? $"{ctx.User.Username}#{ctx.User.Discriminator}" : "Unknown user";
            var channelName = ctx.Channel?.Name ?? "Unknown channel";
            var eventName = ctx.EventName;
            var message = ctx.Message != null ? EscapeTicks(ctx.Message.Content) : string.Empty;

            var contextMessage = $"`{eventName}` by `{user}` in `{channelName}`";

            if (!string.IsNullOrWhiteSpace(message))
                contextMessage += $"{Environment.NewLine}Message: `{message}`";

            var escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

            var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

            return LogError("Failed event", fullErrorMessage);
        }

        public Task LogError(Exception ex, string message)
        {
            return LogError(message, ex.ToString());
        }

        public Task LogError(string errorMessage)
        {
            return LogError("Error", errorMessage);
        }

        public async Task LogError(string error, string errorMessage)
        {
            var guildId = _options.GuildId;
            var errorLogChannelId = _options.ErrorLogChannelId;

            var errorLogChannel = await ResolveErrorLogChannel(guildId, errorLogChannelId);

            if (errorLogChannel == null) return;

            var errorEmbed = EmbedBuilderHelper.BuildSimpleEmbed(error, errorMessage, DiscordConstants.ErrorEmbedColor);

            await errorLogChannel.SendMessageAsync(errorEmbed);
        }

        public async Task LogWarning(string warning, string warningMessage)
        {
            var guildId = _options.GuildId;
            var errorLogChannelId = _options.ErrorLogChannelId;

            var errorLogChannel = await ResolveErrorLogChannel(guildId, errorLogChannelId);

            if (errorLogChannel == null) return;

            var errorEmbed = EmbedBuilderHelper.BuildSimpleEmbed(warning, warningMessage, DiscordConstants.WarningEmbedColor);

            await errorLogChannel.SendMessageAsync(errorEmbed);
        }

        private static string EscapeTicks(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return value.Replace('`', '\'');
        }

        // There is a DiscordResolver method for resolving channels...
        // but that class depends on DiscordLogger so it's just simpler
        // to have a duplicate method than dealing with circular dependency
        private async Task<DiscordChannel> ResolveErrorLogChannel(ulong guildId, ulong channelId)
        {
            _client.Guilds.TryGetValue(guildId, out var discordGuild);

            if (discordGuild == null)
            {
                discordGuild = await _client.GetGuildAsync(guildId);
            }

            discordGuild.Channels.TryGetValue(channelId, out var discordChannel);

            if (discordChannel == null)
            {
                discordChannel = discordGuild.GetChannel(channelId);
            }

            return discordChannel;
        }
    }
}
