using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Common.Models;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;
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
        private readonly ILogger<RoleModule> _logger;
        private readonly BotOptions _options;

        public RoleModule(
            RoleService roleService,
            IOptions<BotOptions> options,
            ILogger<RoleModule> logger
            )
        {
            _roleService = roleService;
            _logger = logger;
            _options = options.Value;
        }

        [Command("roles")]
        public async Task GetRoleList(CommandContext ctx)
        {
            var userRoles = await _roleService.GetRoleList();

            var sb = new StringBuilder();

            sb.AppendLine("**List of roles**");

            var roleGroups = userRoles
                .GroupBy(r => r.GroupNumber)
                .OrderBy(r => r.Key.HasValue ? r.Key : int.MaxValue);

            foreach (var roleGroup in roleGroups)
            {
                foreach (var userRole in roleGroup.ToList())
                {
                    sb.Append($"* {userRole.Name}");

                    if(userRole.UserRoleAliases.Any())
                    {
                        var formattedAliasList = string.Join(", ", userRole.UserRoleAliases.Select(x => x.Alias));
                        sb.Append($" (or {formattedAliasList})");
                    }

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            var message = sb.ToString();

            await ctx.RespondAsync(message);
        }

        [Command("role")]
        public async Task SetSelfRole(CommandContext ctx, [RemainingText] string roleName)
        {
            var member = ctx.Member;

            var userRole = await _roleService.GetUserRoleByNameOrAlias(roleName);

            if(userRole == null)
            {
                await ctx.RespondAsync($"**{roleName}** role does not exist");
                return;
            }

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            // Check if user already has role
            var existingDiscordRole = member.Roles.SingleOrDefault(r => r.Id == userRole.RoleId);

            if (existingDiscordRole != null)
            {
                await RemoveDiscordRole(ctx, existingDiscordRole, userRole.Name);
            }
            else
            {
                await AddDiscordRole(ctx, userRole);
            }
        }

        private async Task AddDiscordRole(CommandContext ctx, UserRole userRole)
        {
            var member = ctx.Member;
            var guild = ctx.Guild;

            // Check that the role exists in the guild
            if (!ctx.Guild.Roles.ContainsKey(userRole.RoleId))
            {
                throw new ArgumentException($"Role Id {userRole.RoleId} does not exist");
            }

            if(member == null)
                throw new ArgumentNullException(nameof(member));

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
                    // Get the user role matching this discord role
                    var userRoleToRemove = rolesInGroup.Single(r => r.RoleId == discordRoleInGroup.Id);

                    await member.RevokeRoleAsync(discordRoleInGroup);

                    await LogRoleRemoved(guild, userRoleToRemove.Name, member);
                }
            }

            // Assign new role
            var guildRole = ctx.Guild.Roles[userRole.RoleId];

            await member.GrantRoleAsync(guildRole);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));

            await LogRoleAdded(guild, userRole.Name, member);
        }

        private async Task RemoveDiscordRole(CommandContext ctx, DiscordRole existingDiscordRole, string userRoleName)
        {
            var member = ctx.Member;
            var guild = ctx.Guild;

            if (member == null)
                throw new ArgumentNullException(nameof(member));

            await member.RevokeRoleAsync(existingDiscordRole);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));

            await LogRoleRemoved(guild, userRoleName, member);
        }

        private async Task LogRoleAdded(DiscordGuild guild, string roleName, DiscordMember member)
        {
            var logChannelId = _options.LogChannelId;

            var logChannel = guild.Channels[logChannelId];

            if(logChannel == null)
            {
                _logger.LogError($"Could not fetch log channel {logChannelId} from guild");
                return;
            }

            await logChannel.SendMessageAsync($"Added role **{roleName}** to **{member.GetNicknameOrDisplayName()}**");
        }

        private async Task LogRoleRemoved(DiscordGuild guild, string roleName, DiscordMember member)
        {
            var logChannelId = _options.LogChannelId;

            var logChannel = guild.Channels[logChannelId];

            if (logChannel == null)
            {
                _logger.LogError($"Could not fetch log channel {logChannelId} from guild");
                return;
            }

            await logChannel.SendMessageAsync($"Removed role **{roleName}** from **{member.GetNicknameOrDisplayName()}**");
        }
    }
}
