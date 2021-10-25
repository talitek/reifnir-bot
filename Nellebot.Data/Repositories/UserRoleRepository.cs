using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Data.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly BotDbContext _dbContext;

        public UserRoleRepository(BotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserRole> CreateRole(ulong roleId, string name)
        {
            var userRole = new UserRole()
            {
                RoleId = roleId,
                Name = name
            };

            await _dbContext.AddAsync(userRole);

            await _dbContext.SaveChangesAsync();

            return userRole;
        }

        public async Task<UserRole> UpdateRole(Guid id, string name)
        {
            var role = await _dbContext.UserRoles.SingleOrDefaultAsync(r => r.Id == id);

            role.Name = name;

            await _dbContext.SaveChangesAsync();

            return role;
        }

        public async Task DeleteRole(Guid id)
        {
            var role = await _dbContext.UserRoles.SingleOrDefaultAsync(r => r.Id == id);

            _dbContext.Remove(role);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<UserRole> GetRoleByDiscordRoleId(ulong roleId)
        {
            var role = await _dbContext.UserRoles
                .Include(x => x.UserRoleAliases)
                .SingleOrDefaultAsync(r => r.RoleId == roleId);

            return role;
        }

        public async Task<IEnumerable<UserRole>> GetRoleList()
        {
            var roles = await _dbContext.UserRoles
                .Include(x => x.UserRoleAliases)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return roles;
        }

        public async Task<UserRoleAlias> GetRoleAlias(string alias)
        {
            var roleAlias = await _dbContext.UserRoleAliases.SingleOrDefaultAsync(ra => ra.Alias == alias);

            return roleAlias;
        }

        public async Task<UserRoleAlias> CreateRoleAlias(Guid userRoleId, string alias)
        {
            var roleAlias = new UserRoleAlias()
            {
                UserRoleId = userRoleId,
                Alias = alias
            };

            await _dbContext.AddAsync(roleAlias);

            await _dbContext.SaveChangesAsync();

            return roleAlias;
        }

        public async Task DeleteRoleAlias(Guid userRoleId, string alias)
        {
            var roleAlias = await _dbContext.UserRoleAliases.SingleOrDefaultAsync(ra => ra.Alias == alias);

            _dbContext.Remove(roleAlias);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<UserRole> UpdateRoleGroup(Guid id, uint? groupNumber)
        {
            var role = await _dbContext.UserRoles.SingleOrDefaultAsync(r => r.Id == id);

            role.GroupNumber = groupNumber;

            await _dbContext.SaveChangesAsync();

            return role;
        }

        public async Task<UserRole> GetRoleByNameOrAlias(string roleName)
        {
            var role = await _dbContext.UserRoles.FirstOrDefaultAsync(x => x.Name.ToLower() == roleName);

            if(role == null)
            {
                role = await _dbContext.UserRoles
                .Include(x => x.UserRoleAliases)
                .SingleOrDefaultAsync(r => r.UserRoleAliases.Select(a => a.Alias).Contains(roleName));
            }            

            return role;
        }

        public async Task<List<UserRole>> GetRolesByGroup(uint groupNumber)
        {
            var roles = await _dbContext.UserRoles
                .Include(x => x.UserRoleAliases)
                .Where(x => x.GroupNumber.HasValue && x.GroupNumber == groupNumber)
                .ToListAsync();

            return roles;
        }
    }
}
