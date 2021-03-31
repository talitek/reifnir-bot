using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [RequireOwnerOrAdmin]
    [Group("user-role")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class UserRoleModule: BaseCommandModule
    {
        private readonly UserRoleService _userRoleService;

        public UserRoleModule(UserRoleService userRoleService)
        {
            _userRoleService = userRoleService;
        }

        [Command("create-role")]
        public async Task CreateRole(CommandContext ctx, DiscordRole role, string name, string aliasList)
        {
            throw new NotImplementedException();
        }

        [Command("update-role")]
        public async Task UpdateRole(CommandContext ctx, DiscordRole role, string name)
        {
            throw new NotImplementedException();
        }

        [Command("delete-role")]
        public async Task DeleteRole(CommandContext ctx, DiscordRole role)
        {
            throw new NotImplementedException();
        }

        [Command("get-role")]
        public async Task GetRole(CommandContext ctx, DiscordRole role)
        {
            throw new NotImplementedException();
        }

        [Command("list-roles")]
        public async Task GetRoleList(CommandContext ctx)
        {
            throw new NotImplementedException();
        }

        [Command("add-alias")]
        public async Task AddRoleAlias(CommandContext ctx, DiscordRole role, string alias)
        {
            throw new NotImplementedException();
        }

        [Command("remove-alias")]
        public async Task RemoveRoleAlias(CommandContext ctx, DiscordRole role, string alias)
        {
            throw new NotImplementedException();
        }

        [Command("set-group")]
        public async Task SetRoleGroup(CommandContext ctx, DiscordRole role, uint groupNumber)
        {
            throw new NotImplementedException();
        }

        [Command("uset-group")]
        public async Task UnsetRoleGroup(CommandContext ctx, DiscordRole role)
        {
            throw new NotImplementedException();
        }
    }
}
