using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.DiscordModelMappers;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.CommandModules.Roles;

[BaseCommandCheck]
[RequireOwnerOrAdmin]
[Group("user-role")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class UserRoleModule : BaseCommandModule
{
    private readonly DiscordResolver _discordResolver;
    private readonly BotOptions _options;
    private readonly UserRoleService _userRoleService;

    public UserRoleModule(
        IOptions<BotOptions> options,
        UserRoleService userRoleService,
        DiscordResolver discordResolver)
    {
        _options = options.Value;
        _userRoleService = userRoleService;
        _discordResolver = discordResolver;
    }

    [Command("list-roles")]
    public async Task GetRoleList(CommandContext ctx)
    {
        var userRoles = await _userRoleService.GetRoleList();

        var sb = new StringBuilder();

        sb.AppendLine("**List of roles**");

        var roleGroups = userRoles
            .GroupBy(r => r.Group)
            .OrderBy(r => r.Key?.Id ?? int.MaxValue);

        foreach (var roleGroup in roleGroups)
        {
            if (roleGroup.Key != null)
            {
                sb.AppendLine(
                              $"{roleGroup.Key.Name} group (group id: {roleGroup.Key.Id} {(roleGroup.Key.MutuallyExclusive ? ", mutually exclusive" : string.Empty)})");
            }
            else
            {
                sb.AppendLine("Ungrouped");
            }

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

    [Command("create-role")]
    public async Task CreateRole(CommandContext ctx, DiscordRole role, [RemainingText] string? aliasList = null)
    {
        var appDiscordRole = DiscordRoleMapper.Map(role);

        await _userRoleService.CreateRole(appDiscordRole, aliasList);

        await ctx.RespondAsync($"Created user role for {role.Name}");
    }

    [Command("create-role")]
    public async Task CreateRole(CommandContext ctx, ulong roleId, [RemainingText] string? aliasList = null)
    {
        ctx.Guild.Roles.TryGetValue(roleId, out var discordRole);

        if (discordRole == null)
        {
            await ctx.RespondAsync($"A discord role with id {roleId} doesn't exist.");
            return;
        }

        await CreateRole(ctx, discordRole, aliasList);
    }

    [Command("create-role")]
    public async Task CreateRole(CommandContext ctx, string discordRoleName, [RemainingText] string? aliasList = null)
    {
        var result = _discordResolver.TryResolveRoleByName(ctx.Guild, discordRoleName);

        if (!result.Resolved)
        {
            await ctx.RespondAsync(result.ErrorMessage);
            return;
        }

        await CreateRole(ctx, result.Value, aliasList);
    }

    [Command("delete-role")]
    public async Task DeleteRole(CommandContext ctx, DiscordRole role)
    {
        var appDiscordRole = DiscordRoleMapper.Map(role);

        await _userRoleService.DeleteRole(appDiscordRole);

        await ctx.RespondAsync($"Deleted user role for {role.Name}");
    }

    [Command("delete-role")]
    public async Task DeleteRole(CommandContext ctx, ulong roleId)
    {
        ctx.Guild.Roles.TryGetValue(roleId, out var discordRole);

        if (discordRole == null)
        {
            await ctx.RespondAsync($"A discord role with id {roleId} doesn't exist.");
            return;
        }

        await DeleteRole(ctx, discordRole);
    }

    [Command("delete-role")]
    public async Task DeleteRole(CommandContext ctx, [RemainingText] string discordRoleName)
    {
        var result = _discordResolver.TryResolveRoleByName(ctx.Guild, discordRoleName);

        if (!result.Resolved)
        {
            await ctx.RespondAsync(result.ErrorMessage);
            return;
        }

        await DeleteRole(ctx, result.Value);
    }

    [Command("add-alias")]
    public async Task AddRoleAlias(CommandContext ctx, DiscordRole role, [RemainingText] string alias)
    {
        var appDiscordRole = DiscordRoleMapper.Map(role);

        await _userRoleService.AddRoleAlias(appDiscordRole, alias);

        await ctx.RespondAsync($"Added alias {alias} for {role.Name}");
    }

    [Command("add-alias")]
    public async Task AddRoleAlias(CommandContext ctx, ulong roleId, [RemainingText] string alias)
    {
        ctx.Guild.Roles.TryGetValue(roleId, out var discordRole);

        if (discordRole == null)
        {
            await ctx.RespondAsync($"A role with id {roleId} doesn't exist.");
            return;
        }

        await AddRoleAlias(ctx, discordRole, alias);
    }

    [Command("add-alias")]
    public async Task AddRoleAlias(CommandContext ctx, string discordRoleName, [RemainingText] string alias)
    {
        var result = _discordResolver.TryResolveRoleByName(ctx.Guild, discordRoleName);

        if (!result.Resolved)
        {
            await ctx.RespondAsync(result.ErrorMessage);
            return;
        }

        await AddRoleAlias(ctx, result.Value, alias);
    }

    [Command("remove-alias")]
    public async Task RemoveRoleAlias(CommandContext ctx, DiscordRole role, [RemainingText] string alias)
    {
        var appDiscordRole = DiscordRoleMapper.Map(role);

        await _userRoleService.RemoveRoleAlias(appDiscordRole, alias);

        await ctx.RespondAsync($"Removed alias {alias} for {role.Name}");
    }

    [Command("remove-alias")]
    public async Task RemoveRoleAlias(CommandContext ctx, ulong roleId, [RemainingText] string alias)
    {
        ctx.Guild.Roles.TryGetValue(roleId, out var discordRole);

        if (discordRole == null)
        {
            await ctx.RespondAsync($"A discord role with id {roleId} doesn't exist.");
            return;
        }

        await RemoveRoleAlias(ctx, discordRole, alias);
    }

    [Command("remove-alias")]
    public async Task RemoveRoleAlias(CommandContext ctx, string discordRoleName, [RemainingText] string alias)
    {
        var result = _discordResolver.TryResolveRoleByName(ctx.Guild, discordRoleName);

        if (!result.Resolved)
        {
            await ctx.RespondAsync(result.ErrorMessage);
            return;
        }

        await RemoveRoleAlias(ctx, result.Value, alias);
    }

    [Command("set-group")]
    public async Task SetRoleGroup(CommandContext ctx, DiscordRole role, uint groupNumber)
    {
        var appDiscordRole = DiscordRoleMapper.Map(role);

        await _userRoleService.SetRoleGroup(appDiscordRole, groupNumber);

        await ctx.RespondAsync($"Set group number {groupNumber} for {role.Name}");
    }

    [Command("set-group")]
    public async Task SetRoleGroup(CommandContext ctx, ulong roleId, uint groupNumber)
    {
        ctx.Guild.Roles.TryGetValue(roleId, out var discordRole);

        if (discordRole == null)
        {
            await ctx.RespondAsync($"A role with id {roleId} doesn't exist.");
            return;
        }

        await SetRoleGroup(ctx, discordRole, groupNumber);
    }

    [Command("set-group")]
    public async Task SetRoleGroup(CommandContext ctx, string discordRoleName, uint groupNumber)
    {
        var result = _discordResolver.TryResolveRoleByName(ctx.Guild, discordRoleName);

        if (!result.Resolved)
        {
            await ctx.RespondAsync(result.ErrorMessage);
            return;
        }

        await SetRoleGroup(ctx, result.Value, groupNumber);
    }

    [Command("unset-group")]
    public async Task UnsetRoleGroup(CommandContext ctx, DiscordRole role)
    {
        var appDiscordRole = DiscordRoleMapper.Map(role);

        await _userRoleService.UnsetRoleGroup(appDiscordRole);

        await ctx.RespondAsync($"Unset group number for {role.Name}");
    }

    [Command("unset-group")]
    public async Task UnsetRoleGroup(CommandContext ctx, ulong roleId)
    {
        ctx.Guild.Roles.TryGetValue(roleId, out var discordRole);

        if (discordRole == null)
        {
            await ctx.RespondAsync($"A role with id {roleId} doesn't exist.");
            return;
        }

        await UnsetRoleGroup(ctx, discordRole);
    }

    [Command("unset-group")]
    public async Task UnsetRoleGroup(CommandContext ctx, string discordRoleName)
    {
        var result = _discordResolver.TryResolveRoleByName(ctx.Guild, discordRoleName);

        if (!result.Resolved)
        {
            await ctx.RespondAsync(result.ErrorMessage);
            return;
        }

        await UnsetRoleGroup(ctx, result.Value);
    }

    [Command("set-group-name")]
    public async Task SetRoleGroupName(CommandContext ctx, uint groupId, [RemainingText] string groupName)
    {
        await _userRoleService.SetRoleGroupName(groupId, groupName);

        await ctx.RespondAsync($"Set group name {groupName} for group {groupId}");
    }

    [Command("set-group-mutex")]
    public async Task SetRoleGroupMutex(CommandContext ctx, uint groupId, bool mutuallyExclusive)
    {
        await _userRoleService.SetRoleMutex(groupId, mutuallyExclusive);

        await ctx.RespondAsync($"Set group mutex flag {mutuallyExclusive} for group {groupId}");
    }

    [Command("delete-group")]
    public async Task DeleteRoleGroup(CommandContext ctx, uint groupId)
    {
        await _userRoleService.DeleteRoleGroup(groupId);

        await ctx.RespondAsync($"Deleted group {groupId}");
    }

    [Command("sync-roles")]
    [Description("Sync user roles with Discord roles (update, delete)")]
    public async Task UpdateRoles(CommandContext ctx)
    {
        var guildRoles = ctx.Guild.Roles.Select(r => DiscordRoleMapper.Map(r.Value));

        var result = await _userRoleService.SyncRoles(guildRoles);

        var updatedCount = result.UpdatedCount;
        var deletedCount = result.DeletedCount;

        if (updatedCount == 0 && deletedCount == 0)
        {
            await ctx.RespondAsync("Roles already up to date");
            return;
        }

        var sb = new StringBuilder();

        if (result.UpdatedCount > 0)
        {
            sb.AppendLine($"Updated {updatedCount} user {(updatedCount == 1 ? "role" : "roles")}");
        }

        if (result.DeletedCount > 0)
        {
            sb.AppendLine($"Deleted {deletedCount} user {(deletedCount == 1 ? "role" : "roles")}");
        }

        await ctx.RespondAsync(sb.ToString());
    }
}
