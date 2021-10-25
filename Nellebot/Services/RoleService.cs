using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class RoleService
    {
        private readonly IUserRoleRepository _userRoleRepo;

        public RoleService(IUserRoleRepository userRoleRepo)
        {
            _userRoleRepo = userRoleRepo;
        }

        public async Task<UserRole> GetUserRoleByNameOrAlias(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("Role name cannot be empty");

            var role = await _userRoleRepo.GetRoleByNameOrAlias(roleName.Trim().ToLower());

            return role;
        }

        public async Task<List<UserRole>> GetUserRolesByGroup(uint groupNumber)
        {
            var roles = await _userRoleRepo.GetRolesByGroup(groupNumber);

            return roles;
        }

        public async Task<IEnumerable<UserRole>> GetRoleList()
        {
            return await _userRoleRepo.GetRoleList();
        }
    }
}
