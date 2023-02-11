using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nellebot.Common.Models.UserRoles;

namespace Nellebot.Data.Repositories
{
    public interface IUserRoleRepository
    {
        Task<UserRole> CreateRole(ulong roleId, string name);

        Task<UserRoleAlias> CreateRoleAlias(Guid userRoleId, string alias);

        Task DeleteRole(Guid userRoleId);

        Task DeleteRoleAlias(Guid userRoleId, string alias);

        Task<UserRole?> GetRoleByDiscordRoleId(ulong roleId);

        Task<UserRoleAlias?> GetRoleAlias(string alias);

        Task<IEnumerable<UserRole>> GetRoleList();

        Task<UserRole> UpdateRole(Guid userRoleId, string name);

        Task<UserRole> UpdateRoleGroup(Guid id, uint? groupNumber);

        Task<UserRole?> GetRoleByNameOrAlias(string alias);

        Task<List<UserRole>> GetRolesByGroup(uint groupNumber);
        Task<UserRoleGroup> UpdateRoleGroupName(uint groupId, string name);
        Task DeleteRoleGroup(uint groupId);
    }
}
