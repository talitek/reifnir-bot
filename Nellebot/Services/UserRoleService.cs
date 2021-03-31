using Nellebot.Common.AppDiscordModels;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class UserRoleService
    {
        private readonly IUserRoleRepository _userRoleRepo;

        public UserRoleService(IUserRoleRepository userRoleRepo)
        {
            _userRoleRepo = userRoleRepo;
        }

        public async Task<UserRole> CreateRole(AppDiscordRole role, string name, string aliasList)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty");

            if (string.IsNullOrWhiteSpace(aliasList))
                throw new ArgumentException("Alias list cannot be empty");

            var aliases = aliasList.Split(',').Where(a => !string.IsNullOrWhiteSpace(a)).ToList();

            if (aliases.Count == 0)
                throw new ArgumentException("Alias list cannot be empty");

            var existingRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (existingRole != null)
                throw new ArgumentException("User role already exists");

            var userRole = await _userRoleRepo.CreateRole(role.Id, name.ToLower());

            var newAliases = new List<UserRoleAlias>();

            foreach (var alias in aliases)
            {
                var newAlias = await _userRoleRepo.CreateRoleAlias(userRole.Id, alias);
                newAliases.Add(newAlias);
            }

            userRole.UserRoleAliases = newAliases;

            return userRole;
        }

        public async Task<UserRole> UpdateRole(AppDiscordRole role, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Role name cannot be empty");

            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            await _userRoleRepo.UpdateRole(userRole.Id, name);

            return userRole;
        }

        public async Task DeleteRole(AppDiscordRole role)
        {
            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            await _userRoleRepo.DeleteRole(userRole.Id);
        }

        public async Task<UserRole> GetRole(AppDiscordRole role)
        {
            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            return userRole;
        }

        public async Task<IEnumerable<UserRole>> GetRoleList()
        {
            return await _userRoleRepo.GetRoleList();
        }

        public async Task<UserRoleAlias> AddRoleAlias(AppDiscordRole role, string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentException("Alias cannot be empty");

            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            var existingRoleAlias = await _userRoleRepo.GetRoleAlias(alias);

            if (existingRoleAlias != null)
                throw new ArgumentException("Alias already exists");

            var roleAlias = await _userRoleRepo.CreateRoleAlias(userRole.Id, alias.ToLower());

            return roleAlias;
        }

        public async Task RemoveRoleAlias(AppDiscordRole role, string alias)
        {
            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            if (userRole.UserRoleAliases.Count() == 1)
                throw new ArgumentException("User role must have at least 1 alias");

            await _userRoleRepo.DeleteRoleAlias(userRole.Id, alias);
        }

        public async Task SetRoleGroup(AppDiscordRole role, uint groupNumber)
        {
            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            await _userRoleRepo.UpdateRoleGroup(userRole.Id, groupNumber);
        }

        public async Task UnsetRoleGroup(AppDiscordRole role)
        {
            var userRole = await _userRoleRepo.GetRoleByDiscordRoleId(role.Id);

            if (userRole == null)
                throw new ArgumentException("User role doesn't exist");

            await _userRoleRepo.UpdateRoleGroup(userRole.Id, null);
        }
    }
}
