﻿using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.DiscordModelMappers
{
    public static class DiscordMemberMapper
    {
        public static AppDiscordMember Map(DiscordMember discordMember)
        {
            var appDiscordMember = new AppDiscordMember();

            appDiscordMember.Id = discordMember.Id;
            appDiscordMember.Roles = discordMember.Roles.Select(DiscordRoleMapper.Map);

            return appDiscordMember;
        }
    }
}
