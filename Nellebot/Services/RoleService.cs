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

        public async Task<UserRole> GetUserRoleByAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Alias cannot be empty");

            var role = await _userRoleRepo.GetRoleByAlias(alias.Trim().ToLower());

            return role;
        }

        public async Task<List<UserRole>> GetUserRoleByGroup(uint groupNumber)
        {
            var roles = await _userRoleRepo.GetRolesByGroup(groupNumber);

            return roles;
        }
    }
}
