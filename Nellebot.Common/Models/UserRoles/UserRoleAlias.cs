using System;

namespace Nellebot.Common.Models.UserRoles;

public class UserRoleAlias
{
    public Guid Id { get; set; }

    public Guid UserRoleId { get; set; }

    public UserRole UserRole { get; set; } = null!;

    public string Alias { get; set; } = null!;
}
