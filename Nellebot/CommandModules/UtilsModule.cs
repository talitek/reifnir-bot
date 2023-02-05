using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[Group("utils")]
public class UtilsModule : BaseCommandModule
{
    private readonly DiscordResolver _discordResolver;
    private readonly DiscordLogger _discordLogger;
    private readonly IDiscordErrorLogger _discordErrorLogger;

    public UtilsModule(DiscordResolver discordResolver, DiscordLogger discordLogger, IDiscordErrorLogger discordErrorLogger)
    {
        _discordLogger = discordLogger;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
    }

    [Command]
    public Task TestLogger(CommandContext ctx)
    {
        _discordLogger.LogGreetingMessage("Test greeting");
        _discordLogger.LogActivityMessage("Test greeting");
        _discordLogger.LogExtendedActivityMessage("Test greeting");
        _discordErrorLogger.LogError("Test error");

        return Task.CompletedTask;
    }

    [Command("role-id")]
    public async Task GetRoleId(CommandContext ctx, string roleName)
    {
        TryResolveResult<DiscordRole> resolveResult = _discordResolver.TryResolveRoleByName(ctx.Guild, roleName);

        if (!resolveResult.Resolved)
        {
            await ctx.RespondAsync(resolveResult.ErrorMessage);
            return;
        }

        await ctx.RespondAsync($"Role {roleName} has id {resolveResult.Value.Id}");
    }

    [Command("emoji-code")]
    public async Task GetEmojiCode(CommandContext ctx, DiscordEmoji emoji)
    {
        bool isUnicodeEmoji = emoji.Id == 0;

        if (!isUnicodeEmoji)
        {
            await ctx.RespondAsync($"Not a unicode emoji");
            return;
        }

        var unicodeEncoding = new UnicodeEncoding(true, false);

        byte[] bytes = unicodeEncoding.GetBytes(emoji.Name);

        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            sb.AppendFormat("{0:X2}", bytes[i]);
        }

        string bytesAsString = sb.ToString();

        var formattedSb = new StringBuilder();

        for (int i = 0; i < sb.Length; i += 4)
        {
            formattedSb.Append($"\\u{bytesAsString.Substring(i, 4)}");
        }

        string result = formattedSb.ToString();

        await ctx.RespondAsync($"`{result}`");
    }
}
