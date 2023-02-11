using System;
using System.Collections.Generic;

namespace Nellebot.Common.Models.UserRoles;

public class UserRole
{
    public Guid Id { get; set; }

    public ulong RoleId { get; set; }

    public string Name { get; set; } = null!;

    public virtual UserRoleGroup? Group { get; set; }

    public virtual IEnumerable<UserRoleAlias> UserRoleAliases { get; set; }

    public UserRole()
    {
        UserRoleAliases = new List<UserRoleAlias>();
    }
}
