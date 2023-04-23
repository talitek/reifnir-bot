using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Nellebot.Helpers;

namespace Nellebot.Utils;

public static class DiscordExtensions
{
    public static string GetNicknameOrDisplayName(this DiscordMember member)
    {
        var username = !string.IsNullOrWhiteSpace(member.Nickname)
            ? member.Nickname
            : member.DisplayName;

        return username;
    }

    public static string GetDetailedMemberIdentifier(this DiscordMember member)
    {
        return !string.IsNullOrWhiteSpace(member.Username)
            ? $"{member.Username}#{member.Discriminator}, User Id: {member.Id}"
            : string.Empty;
    }

    public static string GetFullUsername(this DiscordUser user)
    {
        return !string.IsNullOrWhiteSpace(user.Username)
            ? $"{user.Username}#{user.Discriminator}"
            : string.Empty;
    }

    public static Task CreateSuccessReactionAsync(this DiscordMessage message)
    {
        return message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
    }

    public static Task CreateFailureReactionAsync(this DiscordMessage message)
    {
        return message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
    }

    public static string GetQuotedContent(this DiscordMessage message)
    {
        var lines = message.Content.Split(DiscordConstants.NewLineChar).ToList();

        var quotedLines = lines.Select(line => $"> {line}");

        return string.Join(DiscordConstants.NewLineChar, quotedLines);
    }
}
