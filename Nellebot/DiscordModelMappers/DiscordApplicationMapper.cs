using System.Linq;
using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordApplicationMapper
{
    public static AppDiscordApplication Map(DiscordApplication discordApplication)
    {
        var appDiscordApplication = new AppDiscordApplication();

        // The owner list can be empty due to a bug in the DSharp library
        // Should be fixed soon. TODO: Remove this check when fixed.
        appDiscordApplication.Owners = discordApplication.Owners?.Select(DiscordUserMapper.Map);

        return appDiscordApplication;
    }
}
