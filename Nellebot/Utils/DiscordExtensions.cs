using DSharpPlus.Entities;

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
        return $"{member.Username}#{member.Discriminator}, User Id: {member.Id}";
    }
}