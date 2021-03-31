using Nellebot.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Data.Repositories
{
    public interface IUserRoleRepository
    {
        Task<UserRole> CreateRole(ulong roleId, string name);
        Task<UserRoleAlias> CreateRoleAlias(Guid userRoleId, string alias);
        Task DeleteRole(Guid userRoleId);
        Task DeleteRoleAlias(Guid userRoleId, string alias);
        Task<UserRole> GetRole(ulong roleId);
        Task<UserRoleAlias> GetRoleAlias(string alias);
        Task<IEnumerable<UserRole>> GetRoleList();
        Task<UserRole> UpdateRole(Guid userRoleId, string name);
        Task UpdateRoleGroup(Guid userRoleId, uint? groupNumber);
    }

    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly BotDbContext _dbContext;

        public UserRoleRepository(BotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserRole> CreateRole(ulong roleId, string name)
        {
            throw new NotImplementedException();
        }

        public async Task<UserRole> UpdateRole(Guid userRoleId, string name)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteRole(Guid userRoleId)
        {
            throw new NotImplementedException();
        }

        public async Task<UserRole> GetRole(ulong roleId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<UserRole>> GetRoleList()
        {
            throw new NotImplementedException();
        }

        public async Task<UserRoleAlias> GetRoleAlias(string alias)
        {
            throw new NotImplementedException();
        }

        public async Task<UserRoleAlias> CreateRoleAlias(Guid userRoleId, string alias)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteRoleAlias(Guid userRoleId, string alias)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateRoleGroup(Guid userRoleId, uint? groupNumber)
        {
            throw new NotImplementedException();
        }
    }
}
