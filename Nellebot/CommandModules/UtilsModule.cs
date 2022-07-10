using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    [Group("utils")]
    public class UtilsModule : BaseCommandModule
    {
        private readonly DiscordResolver _discordResolver;
        private readonly IDiscordErrorLogger _discordErrorLogger;

        public UtilsModule(DiscordResolver discordResolver, IDiscordErrorLogger discordErrorLogger)
        {
            _discordResolver = discordResolver;
            _discordErrorLogger = discordErrorLogger;
        }

        [Command("role-id")]
        public async Task GetRoleId(CommandContext ctx, string roleName)
        {
            var result = _discordResolver.TryResolveRoleByName(ctx.Guild, roleName, out var discordRole);

            if (!result.Resolved)
            {
                await ctx.RespondAsync(result.ErrorMessage);
                return;
            }

            await ctx.RespondAsync($"Role {roleName} has id {discordRole.Id}");
        }

        [Command("emoji-code")]
        public async Task GetEmojiCode(CommandContext ctx, DiscordEmoji emoji)
        {
            var isUnicodeEmoji = emoji.Id == 0;

            if (!isUnicodeEmoji)
            {
                await ctx.RespondAsync($"Not a unicode emoji");
                return;
            }

            var unicodeEncoding = new UnicodeEncoding(true, false);

            var bytes = unicodeEncoding.GetBytes(emoji.Name);

            var sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.AppendFormat("{0:X2}", bytes[i]);
            }

            var bytesAsString = sb.ToString();

            var formattedSb = new StringBuilder();

            for (int i = 0; i < sb.Length; i += 4)
            {
                formattedSb.Append($"\\u{bytesAsString.Substring(i, 4)}");
            }

            var result = formattedSb.ToString();

            await ctx.RespondAsync($"`{result}`");
        }

        [Command("test-error")]
        public async Task TestError(CommandContext ctx)
        {
            await _discordErrorLogger.LogError("Test error", "Test error message");
            await _discordErrorLogger.LogWarning("Test warning", "Test warning message");
        }
    }
}
