using System.Collections.Generic;

namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordMember : AppDiscordUser
{
    public string? Nickname { get; set; }
    public IEnumerable<AppDiscordRole> Roles { get; set; } = null!;
}
