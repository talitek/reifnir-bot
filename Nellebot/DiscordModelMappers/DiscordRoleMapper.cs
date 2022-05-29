using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.DiscordModelMappers
{
    public static class DiscordRoleMapper
    {
        public static AppDiscordRole Map(DiscordRole discordRole)
        {
            var appDiscordRole = new AppDiscordRole();

            appDiscordRole.Id = discordRole.Id;
            appDiscordRole.Name = discordRole.Name;

            return appDiscordRole;
        }
    }
}
