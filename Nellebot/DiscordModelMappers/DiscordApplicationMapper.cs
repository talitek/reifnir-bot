using System.Linq;
using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordApplicationMapper
{
    public static AppDiscordApplication Map(DiscordApplication discordApplication)
    {
        var appDiscordApplication = new AppDiscordApplication();

        appDiscordApplication.Owners = discordApplication.Owners.Select(DiscordUserMapper.Map);

        return appDiscordApplication;
    }
}
