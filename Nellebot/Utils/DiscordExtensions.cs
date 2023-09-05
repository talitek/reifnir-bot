using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Nellebot.Helpers;

namespace Nellebot.Utils;

public static class DiscordExtensions
{
    [Obsolete("Function implemented in D# library.")]
    public static string GetNicknameOrDisplayName(this DiscordMember member)
    {
        return member.DisplayName;
    }

    public static string GetDetailedUserIdentifier(this DiscordUser user)
    {
        return $"{user.GetFullUsername()} ({user.Id})";
    }

    public static string GetDetailedMemberIdentifier(this DiscordMember member)
    {
        var memberUsername = member.Username;
        var memberDisplayName = member.DisplayName;

        var memberFormattedDisplayName = memberUsername != memberDisplayName
                ? $"{member.DisplayName} ({member.GetFullUsername()}, {member.Id})"
                : $"{member.GetFullUsername()} ({member.Id})";

        return memberFormattedDisplayName;
    }

    public static string GetFullUsername(this DiscordUser user)
    {
        return user.HasLegacyUsername() ? $"{user.Username}#{user.Discriminator}" : user.Username;
    }

    private static bool HasLegacyUsername(this DiscordUser user)
    {
        return user.Discriminator == "0";
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

    public static string NullOrWhiteSpaceTo(this string input, string fallback)
    {
        return !string.IsNullOrWhiteSpace(input) ? input : fallback;
    }
}
