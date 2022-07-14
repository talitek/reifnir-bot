using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models
{
    public class UserRole
    {
        public Guid Id { get; set; }
        public ulong RoleId { get; set; }
        public string Name { get; set; } = null!;
        public uint? GroupNumber { get; set; }
        public virtual IEnumerable<UserRoleAlias> UserRoleAliases { get; set; }

        public UserRole()
        {
            UserRoleAliases = new List<UserRoleAlias>();
        }
    }
}
