namespace Nellebot.Common.Models.UserRoles;

public class UserRoleGroup
{
    public uint Id { get; set; }

    public string Name { get; set; } = null!;

    public bool MutuallyExclusive { get; set; } = true;
}
