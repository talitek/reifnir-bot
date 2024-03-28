using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordRoleMapper
{
    public static AppDiscordRole Map(DiscordRole discordRole)
    {
        var appDiscordRole = new AppDiscordRole();

        appDiscordRole.Id = discordRole.Id;
        appDiscordRole.Name = discordRole.Name;

        return appDiscordRole;
    }
}
