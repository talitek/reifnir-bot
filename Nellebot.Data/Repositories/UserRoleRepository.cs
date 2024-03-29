using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models.UserRoles;

namespace Nellebot.Data.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly BotDbContext _dbContext;

    public UserRoleRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserRole> CreateRole(ulong roleId, string name)
    {
        var userRole = new UserRole
        {
            RoleId = roleId,
            Name = name,
        };

        _dbContext.Add(userRole);

        await _dbContext.SaveChangesAsync();

        return userRole;
    }

    public async Task<UserRole> UpdateRole(Guid id, string name)
    {
        UserRole? role = await _dbContext.UserRoles.SingleOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            throw new Exception("Role not found");
        }

        role.Name = name;

        await _dbContext.SaveChangesAsync();

        return role;
    }

    public async Task DeleteRole(Guid id)
    {
        UserRole? role = await _dbContext.UserRoles.SingleOrDefaultAsync(r => r.Id == id);

        if (role == null)
        {
            return;
        }

        _dbContext.Remove(role);

        await _dbContext.SaveChangesAsync();
    }

    public Task<UserRole?> GetRoleByDiscordRoleId(ulong roleId)
    {
        return _dbContext.UserRoles
            .Include(x => x.UserRoleAliases)
            .Include(x => x.Group)
            .SingleOrDefaultAsync(r => r.RoleId == roleId);
    }

    public async Task<IEnumerable<UserRole>> GetRoleList()
    {
        List<UserRole> roles = await _dbContext.UserRoles
            .Include(x => x.UserRoleAliases)
            .Include(x => x.Group)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return roles;
    }

    public Task<UserRoleAlias?> GetRoleAlias(string alias)
    {
        return _dbContext.UserRoleAliases.SingleOrDefaultAsync(ra => ra.Alias == alias);
    }

    public async Task<UserRoleAlias> CreateRoleAlias(Guid userRoleId, string alias)
    {
        var roleAlias = new UserRoleAlias
        {
            UserRoleId = userRoleId,
            Alias = alias,
        };

        await _dbContext.AddAsync(roleAlias);

        await _dbContext.SaveChangesAsync();

        return roleAlias;
    }

    public async Task DeleteRoleAlias(Guid userRoleId, string alias)
    {
        UserRoleAlias? roleAlias = await _dbContext.UserRoleAliases.SingleOrDefaultAsync(ra => ra.Alias == alias);

        if (roleAlias == null)
        {
            return;
        }

        _dbContext.Remove(roleAlias);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<UserRole> UpdateRoleGroup(Guid id, uint? groupId)
    {
        UserRole? role = await _dbContext.UserRoles.FindAsync(id);

        if (role == null)
        {
            throw new Exception("Role not found");
        }

        if (!groupId.HasValue)
        {
            role.Group = null;
            await _dbContext.SaveChangesAsync();
            return role;
        }

        UserRoleGroup? roleGroup = await _dbContext.UserRoleGroups.FindAsync(groupId);

        // If role group doesn't exist, create it
        if (roleGroup == null)
        {
            roleGroup = new UserRoleGroup
            {
                Id = groupId.Value,
                Name = $"Untitled {groupId}",
            };

            await _dbContext.AddAsync(roleGroup);
        }

        role.Group = roleGroup;

        await _dbContext.SaveChangesAsync();

        return role;
    }

    public async Task<UserRole?> GetRoleByNameOrAlias(string roleName)
    {
        UserRole? role = await _dbContext.UserRoles.FirstOrDefaultAsync(x => x.Name.ToLower() == roleName);

        role ??= await _dbContext.UserRoles
            .Include(x => x.UserRoleAliases)
            .Include(x => x.Group)
            .SingleOrDefaultAsync(r => r.UserRoleAliases.Select(a => a.Alias).Contains(roleName));

        return role;
    }

    public Task<List<UserRole>> GetRolesByGroup(uint groupNumber)
    {
        return _dbContext.UserRoles
            .Include(x => x.UserRoleAliases)
            .Where(x => x.Group != null && x.Group.Id == groupNumber)
            .ToListAsync();
    }

    public async Task<UserRoleGroup> UpdateRoleGroupName(uint groupId, string name)
    {
        UserRoleGroup? roleGroup = await _dbContext.UserRoleGroups.FindAsync(groupId);

        if (roleGroup == null)
        {
            throw new Exception("Role doesn't exist");
        }

        roleGroup.Name = name;

        await _dbContext.SaveChangesAsync();

        return roleGroup;
    }

    public async Task<UserRoleGroup> UpdateRoleGroupMutext(uint groupId, bool mutuallyExclusive)
    {
        UserRoleGroup? roleGroup = await _dbContext.UserRoleGroups.FindAsync(groupId);

        if (roleGroup == null)
        {
            throw new Exception("Role doesn't exist");
        }

        roleGroup.MutuallyExclusive = mutuallyExclusive;

        await _dbContext.SaveChangesAsync();

        return roleGroup;
    }

    public async Task DeleteRoleGroup(uint groupId)
    {
        UserRoleGroup? roleGroup = await _dbContext.UserRoleGroups.FindAsync(groupId);

        if (roleGroup == null)
        {
            return;
        }

        _dbContext.Remove(roleGroup);

        await _dbContext.SaveChangesAsync();
    }
}
