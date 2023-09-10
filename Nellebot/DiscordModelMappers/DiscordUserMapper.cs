using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers
{
    public static class DiscordUserMapper
    {
        public static AppDiscordUser Map(DiscordUser discordUser)
        {
            var appDiscordUser = new AppDiscordUser();

            appDiscordUser.Id = discordUser.Id;
            appDiscordUser.Username = discordUser.Username;

            return appDiscordUser;
        }
    }
}
