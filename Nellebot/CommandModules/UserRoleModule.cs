﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck, RequireOwnerOrAdmin]
    [Group("user-role")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class UserRoleModule : BaseCommandModule
    {
        private readonly BotOptions _options;
        private readonly UserRoleService _userRoleService;

        public UserRoleModule(
            IOptions<BotOptions> options,
            UserRoleService userRoleService
            )
        {
            _options = options.Value;
            _userRoleService = userRoleService;
        }

        [Command("create-role")]
        public async Task CreateRole(CommandContext ctx, DiscordRole role, string name, string aliasList)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.CreateRole(appDiscordRole, name, aliasList);

            await ctx.RespondAsync($"Created user role with name {name} for {role.Name}");
        }

        [Command("update-role")]
        public async Task UpdateRole(CommandContext ctx, DiscordRole role, string name)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.UpdateRole(appDiscordRole, name);

            await ctx.RespondAsync($"Changed user role name to {name} for {role.Name}");
        }

        [Command("delete-role")]
        public async Task DeleteRole(CommandContext ctx, DiscordRole role)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.DeleteRole(appDiscordRole);

            await ctx.RespondAsync($"Deleted user role for {role.Name}");
        }

        [Command("get-role")]
        public async Task GetRole(CommandContext ctx, DiscordRole role)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            var userRole = await _userRoleService.GetRole(appDiscordRole);

            var sb = new StringBuilder();

            var formattedAliasList = string.Join(", ", userRole.UserRoleAliases.Select(x => x.Alias));
            var groupNumberString = userRole.GroupNumber.HasValue ? userRole.GroupNumber.Value.ToString() : "None";

            sb.AppendLine($"**{userRole.Name}**");
            sb.AppendLine($"Discord role: {role.Name} ({role.Id})");
            sb.AppendLine($"Aliases: {formattedAliasList}");
            sb.AppendLine($"Group number: {groupNumberString}");

            var message = sb.ToString();

            await ctx.RespondAsync(message);
        }

        [Command("list-roles")]
        public async Task GetRoleList(CommandContext ctx)
        {
            var userRoles = await _userRoleService.GetRoleList();

            var sb = new StringBuilder();

            sb.AppendLine("**List of roles**");

            var roleGroups = userRoles
                .GroupBy(r => r.GroupNumber)
                .OrderBy(r => r.Key.HasValue ? r.Key : int.MaxValue);

            foreach (var roleGroup in roleGroups)
            {
                if (roleGroup.Key.HasValue)
                    sb.AppendLine($"Group number {roleGroup.Key.Value}");
                else
                    sb.AppendLine($"Ungrouped");

                foreach (var userRole in roleGroup.ToList())
                {
                    var formattedAliasList = string.Join(", ", userRole.UserRoleAliases.Select(x => x.Alias));

                    sb.AppendLine($"* {userRole.Name}: {formattedAliasList}");
                }
            }

            var message = sb.ToString();

            await ctx.RespondAsync(message);
        }

        [Command("add-alias")]
        public async Task AddRoleAlias(CommandContext ctx, DiscordRole role, string alias)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.AddRoleAlias(appDiscordRole, alias);

            await ctx.RespondAsync($"Added alias {alias} for {role.Name}");
        }

        [Command("remove-alias")]
        public async Task RemoveRoleAlias(CommandContext ctx, DiscordRole role, string alias)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.RemoveRoleAlias(appDiscordRole, alias);

            await ctx.RespondAsync($"Removed alias {alias} for {role.Name}");
        }

        [Command("set-group")]
        public async Task SetRoleGroup(CommandContext ctx, DiscordRole role, uint groupNumber)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.SetRoleGroup(appDiscordRole, groupNumber);

            await ctx.RespondAsync($"Set group number {groupNumber} for {role.Name}");
        }

        [Command("unset-group")]
        public async Task UnsetRoleGroup(CommandContext ctx, DiscordRole role)
        {
            var appDiscordRole = DiscordRoleMapper.Map(role);

            await _userRoleService.UnsetRoleGroup(appDiscordRole);

            await ctx.RespondAsync($"Unset group number for {role.Name}");
        }

        [Command("help")]
        public Task GetHelp(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var command = $"{_options.CommandPrefix}user-role";

            sb.AppendLine("User role commands");

            sb.AppendLine($"`{command} create-role [role] [role-name] [alias-list]`");
            sb.AppendLine($"`{command} update-role [role] [role-name]`");
            sb.AppendLine($"`{command} delete-role [role]`");
            sb.AppendLine($"`{command} get-role [role]`");
            sb.AppendLine($"`{command} list-roles`");
            sb.AppendLine($"`{command} add-alias [role] [alias-name]`");
            sb.AppendLine($"`{command} remove-alias [role] [alias-name]`");
            sb.AppendLine($"`{command} set-group [role] [group-number]`");
            sb.AppendLine($"`{command} unset-group [role]`");

            sb.AppendLine();
            sb.AppendLine($"Command arguments:");
            sb.AppendLine($"`role           .. Discord role @mention, Discord role id or Discord role name`");
            sb.AppendLine($"`                  (Only supports role @mention atm)`");
            sb.AppendLine($"`role-name      .. Friendly role name (e.g. used in #log)`");
            sb.AppendLine($"`alias-name     .. User role alias (used when assigning role)`");
            sb.AppendLine($"`alias-list     .. Comma separated value of alias names`");            
            sb.AppendLine($"`group-number   .. User role group number (positive whole number)`");

            var message = sb.ToString();

            return ctx.RespondAsync(message);
        }
    }
}
