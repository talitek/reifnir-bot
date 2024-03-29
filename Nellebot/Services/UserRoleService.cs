using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models.UserRoles;
using Nellebot.Data.Repositories;

namespace Nellebot.Services;

public class UserRoleService
{
    private readonly IUserRoleRepository _userRoleRepo;

    public UserRoleService(IUserRoleRepository userRoleRepo)
    {
        _userRoleRepo = userRoleRepo;
    }

    public async Task<UserRole> CreateRole(AppDiscordRole role, string? aliasList)
    {
        UserRole? existingRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (existingRole != null)
        {
            throw new ArgumentException("User role already exists");
        }

        UserRole userRole = await _userRoleRepo.CreateRole(role.Id, role.Name);

        if (aliasList != null)
        {
            List<string> aliases = aliasList.Split(',').Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

            var newAliases = new List<UserRoleAlias>();

            foreach (string alias in aliases)
            {
                string aliasName = alias.Trim().ToLower();
                UserRoleAlias newAlias = await _userRoleRepo.CreateRoleAlias(userRole.Id, aliasName);
                newAliases.Add(newAlias);
            }

            userRole.UserRoleAliases = newAliases;
        }

        return userRole;
    }

    public async Task DeleteRole(AppDiscordRole role)
    {
        UserRole? userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (userRole == null)
        {
            throw new ArgumentException("User role doesn't exist");
        }

        await _userRoleRepo.DeleteRole(userRole.Id);
    }

    public async Task<UserRole> GetRole(AppDiscordRole role)
    {
        UserRole? userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (userRole == null)
        {
            throw new ArgumentException("User role doesn't exist");
        }

        return userRole;
    }

    public async Task<IEnumerable<UserRole>> GetRoleList()
    {
        return await _userRoleRepo.GetRoleList();
    }

    public async Task<UserRoleAlias> AddRoleAlias(AppDiscordRole role, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            throw new ArgumentException("Alias cannot be empty");
        }

        UserRole? userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (userRole == null)
        {
            throw new ArgumentException("User role doesn't exist");
        }

        string aliasName = alias.Trim().ToLower();

        UserRoleAlias? existingRoleAlias = await _userRoleRepo.GetRoleAlias(aliasName);

        if (existingRoleAlias != null)
        {
            throw new ArgumentException("Alias already exists");
        }

        UserRoleAlias roleAlias = await _userRoleRepo.CreateRoleAlias(userRole.Id, aliasName);

        return roleAlias;
    }

    public async Task RemoveRoleAlias(AppDiscordRole role, string alias)
    {
        UserRole? userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (userRole == null)
        {
            throw new ArgumentException("User role doesn't exist");
        }

        if (!userRole.UserRoleAliases.Any())
        {
            throw new ArgumentException("User role has no aliases");
        }

        await _userRoleRepo.DeleteRoleAlias(userRole.Id, alias);
    }

    public async Task SetRoleGroup(AppDiscordRole role, uint groupNumber)
    {
        UserRole? userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (userRole == null)
        {
            throw new ArgumentException("User role doesn't exist");
        }

        await _userRoleRepo.UpdateRoleGroup(userRole.Id, groupNumber);
    }

    public async Task UnsetRoleGroup(AppDiscordRole role)
    {
        UserRole? userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

        if (userRole == null)
        {
            throw new ArgumentException("User role doesn't exist");
        }

        await _userRoleRepo.UpdateRoleGroup(userRole.Id, null);
    }

    public Task SetRoleGroupName(uint groupId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role group name cannot be empty");
        }

        return _userRoleRepo.UpdateRoleGroupName(groupId, name);
    }

    public Task SetRoleMutex(uint groupId, bool mutuallyExclusive)
    {
        return _userRoleRepo.UpdateRoleGroupMutext(groupId, mutuallyExclusive);
    }

    public async Task DeleteRoleGroup(uint groupId)
    {
        List<UserRole> rolesInGroup = await _userRoleRepo.GetRolesByGroup(groupId);

        if (rolesInGroup.Any())
        {
            throw new ArgumentException("Cannot delete role group with roles in it");
        }

        await _userRoleRepo.DeleteRoleGroup(groupId);
    }

    public async Task<SyncRolesResult> SyncRoles(IEnumerable<AppDiscordRole> guildRoles)
    {
        IEnumerable<UserRole> userRoles = await _userRoleRepo.GetRoleList();

        uint updatedRoleCount = 0;
        uint deletedRoleCount = 0;

        foreach (UserRole userRole in userRoles)
        {
            AppDiscordRole? discordRole = guildRoles.SingleOrDefault(r => r.Id == userRole.RoleId);

            if (discordRole == null)
            {
                await _userRoleRepo.DeleteRole(userRole.Id);
                deletedRoleCount++;
                continue;
            }

            if (!string.Equals(discordRole.Name, userRole.Name, StringComparison.OrdinalIgnoreCase))
            {
                await _userRoleRepo.UpdateRole(userRole.Id, discordRole.Name);
                updatedRoleCount++;
            }
        }

        return new SyncRolesResult(updatedRoleCount, deletedRoleCount);
    }

    public record SyncRolesResult(uint UpdatedCount, uint DeletedCount);
}
