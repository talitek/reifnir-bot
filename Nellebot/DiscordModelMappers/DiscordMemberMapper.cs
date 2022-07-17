using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;
using System.Linq;

namespace Nellebot.DiscordModelMappers
{
    public static class DiscordMemberMapper
    {
        public static AppDiscordMember Map(DiscordMember discordMember)
        {
            var appDiscordUser = DiscordUserMapper.Map(discordMember);

            var appDiscordMember = new AppDiscordMember();
            appDiscordMember.Id = appDiscordUser.Id;
            appDiscordMember.Username = appDiscordUser.Username;

            appDiscordMember.Nickname = discordMember.Nickname;
            appDiscordMember!.Roles = discordMember.Roles.Select(DiscordRoleMapper.Map);

            return appDiscordMember;
        }
    }
}
