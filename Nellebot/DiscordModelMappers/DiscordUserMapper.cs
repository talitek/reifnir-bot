using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.DiscordModelMappers
{
    public static class DiscordUserMapper
    {
        public static AppDiscordUser Map(DiscordUser discordUser)
        {
            var appDiscordMember = new AppDiscordUser();

            appDiscordMember.Id = discordUser.Id;

            return appDiscordMember;
        }
    }
}
