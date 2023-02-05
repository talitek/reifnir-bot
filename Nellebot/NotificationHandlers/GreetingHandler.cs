using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class GreetingHandler :
    INotificationHandler<GuildMemberAddedNotification>,
    INotificationHandler<GuildMemberRemovedNotification>
{
    private readonly DiscordLogger _discordLogger;
    private readonly BotSettingsService _botSettingsService;

    public GreetingHandler(DiscordLogger discordLogger, BotSettingsService botSettingsService)
    {
        _discordLogger = discordLogger;
        _botSettingsService = botSettingsService;
    }

    public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
    {
        string memberMention = notification.EventArgs.Member.Mention;

        string? greetingMessage = await _botSettingsService.GetGreetingsMessage(memberMention);

        if (greetingMessage == null)
        {
            throw new Exception("Could not load greeting message");
        }

        _discordLogger.LogGreetingMessage(greetingMessage);
    }

    public Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
    {
        string memberName = notification.EventArgs.Member.GetNicknameOrDisplayName();

        _discordLogger.LogGreetingMessage($"**{memberName}** has left the server. Goodbye!");

        return Task.CompletedTask;
    }
}
