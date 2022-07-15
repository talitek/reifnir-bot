using DSharpPlus.Entities;
using Nellebot.Common.AppDiscordModels;

namespace Nellebot.DiscordModelMappers;

public static class DiscordMessageMapper
{
    public static AppDiscordMessage Map(DiscordMessage discordMessage)
    {
        var appDiscordMessage = new AppDiscordMessage();

        appDiscordMessage.Author = DiscordUserMapper.Map(discordMessage.Author);
        appDiscordMessage.Content = discordMessage.Content;

        return appDiscordMessage;
    }
}
