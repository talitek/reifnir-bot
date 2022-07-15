namespace Nellebot.Common.AppDiscordModels;

public class AppDiscordMessage
{
    public AppDiscordUser Author { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
}
