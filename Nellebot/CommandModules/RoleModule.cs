using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.Common.Models;
using Nellebot.Helpers;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class RoleModule : BaseCommandModule
    {
        private readonly RoleService _roleService;

        public RoleModule(RoleService roleService)
        {
            _roleService = roleService;
        }

        [Command("role")]
        public async Task SetSelfRole(CommandContext ctx, string alias)
        {
            var member = ctx.Member;

            var userRole = await _roleService.GetUserRoleByAlias(alias);

            if(userRole == null)
            {
                await ctx.RespondAsync($"{alias} role does not exist");
            }

            // Check if user already has role
            var existingDiscordRole = member.Roles.SingleOrDefault(r => r.Id == userRole.RoleId);

            if (existingDiscordRole != null)
            {
                await RemoveDiscordRole(ctx, existingDiscordRole);
            }
            else
            {
                await AddDiscordRole(ctx, userRole);
            }
        }

        private async Task AddDiscordRole(CommandContext ctx, UserRole userRole)
        {
            var member = ctx.Member;

            // If user role has group, get all roles from the same group
            if (userRole.GroupNumber.HasValue)
            {
                var rolesInGroup = await _roleService.GetUserRolesByGroup(userRole.GroupNumber.Value);

                // If user has a discord role from this group, remove it
                var discordRoleInGroup = member.Roles
                                    .Where(r => rolesInGroup.Select(r => r.RoleId)
                                                .ToList()
                                                .Contains(r.Id))
                                    .FirstOrDefault();

                if (discordRoleInGroup != null)
                {
                    await member.RevokeRoleAsync(discordRoleInGroup);
                }
            }

            // Assign new role
            if (ctx.Guild.Roles.ContainsKey(userRole.RoleId))
            {
                var guildRole = ctx.Guild.Roles[userRole.RoleId];

                await member.GrantRoleAsync(guildRole);

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
            }
            else
            {
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.RedX));
            }
        }

        private static async Task RemoveDiscordRole(CommandContext ctx, DiscordRole existingDiscordRole)
        {
            var member = ctx.Member;

            await member.RevokeRoleAsync(existingDiscordRole);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        }
    }
}
