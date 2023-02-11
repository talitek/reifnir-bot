using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Common.Models.UserRoles;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;

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
            ILogger<RoleModule> logger)
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
                .GroupBy(r => r.Group)
                .OrderBy(r => r.Key?.Id ?? int.MaxValue);

            foreach (var roleGroup in roleGroups)
            {
                if (roleGroup.Key != null)
                    sb.AppendLine($"{roleGroup.Key.Name} group");
                else
                    sb.AppendLine($"Ungrouped");

                foreach (var userRole in roleGroup.ToList())
                {
                    sb.Append($"* {userRole.Name}");

                    if (userRole.UserRoleAliases.Any())
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

            if (userRole == null)
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
            var guild = ctx.Guild;

            // Check that the role exists in the guild
            if (!ctx.Guild.Roles.ContainsKey(userRole.RoleId))
            {
                throw new ArgumentException($"Role Id {userRole.RoleId} does not exist");
            }

            if (member == null)
                throw new Exception($"{nameof(ctx.Member)} is null");

            // Assign new role
            var discordRole = ctx.Guild.Roles[userRole.RoleId];

            await member.GrantRoleAsync(discordRole);

            await LogRoleAdded(guild, discordRole.Name, member);

            // If user role has group, remove all other roles from the same group
            await RemoveAllOtherRolesInGroup(userRole, member, guild);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        }

        private async Task RemoveAllOtherRolesInGroup(UserRole userRole, DiscordMember member, DiscordGuild guild)
        {
            if (userRole.Group == null)
                return;

            var otherRolesInGroup = (await _roleService.GetUserRolesByGroup(userRole.Group.Id))
                                                .Where(r => r.Id != userRole.Id);

            var discordRolesInGroup = member.Roles
                                .Where(r => otherRolesInGroup.Select(r => r.RoleId)
                                            .ToList()
                                            .Contains(r.Id))
                                .ToList();

            foreach (var discordRoleToRemove in discordRolesInGroup)
            {
                await member.RevokeRoleAsync(discordRoleToRemove);

                // Get the user role matching this discord role
                var userRoleToRemove = otherRolesInGroup.Single(r => r.RoleId == discordRoleToRemove.Id);

                await LogRoleRemoved(guild, userRoleToRemove.Name, member);
            }
        }

        private async Task RemoveDiscordRole(CommandContext ctx, DiscordRole existingDiscordRole)
        {
            var member = ctx.Member;
            var guild = ctx.Guild;

            if (member == null)
                throw new Exception($"{nameof(ctx.Member)} is null");

            await member.RevokeRoleAsync(existingDiscordRole);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));

            await LogRoleRemoved(guild, existingDiscordRole.Name, member);
        }

        private async Task LogRoleAdded(DiscordGuild guild, string roleName, DiscordMember member)
        {
            var logChannelId = _options.ActivityLogChannelId;

            var logChannel = guild.Channels[logChannelId];

            if (logChannel == null)
            {
                _logger.LogError($"Could not fetch log channel {logChannelId} from guild");
                return;
            }

            await logChannel.SendMessageAsync($"Added role **{roleName}** to **{member.GetNicknameOrDisplayName()}**");
        }

        private async Task LogRoleRemoved(DiscordGuild guild, string roleName, DiscordMember member)
        {
            var logChannelId = _options.ActivityLogChannelId;

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
