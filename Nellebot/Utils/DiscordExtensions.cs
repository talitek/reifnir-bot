using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nellebot.Utils
{
    public static class DiscordExtensions
    {
        public static string GetNicknameOrDisplayName(this DiscordMember member)
        {
            var username = !string.IsNullOrWhiteSpace(member.Nickname)
                ? member.Nickname
                : member.DisplayName;

            return username;
        }
    }
}
