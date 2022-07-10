using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

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

    public static string GetUserFullUsername(this DiscordUser user)
    {
        return !string.IsNullOrWhiteSpace(user.Username)
            ? $"{user.Username}#{user.Discriminator}"
            : string.Empty;
    }
}