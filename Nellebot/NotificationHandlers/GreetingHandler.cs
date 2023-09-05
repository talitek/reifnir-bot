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
    private readonly GoodbyeMessageBuffer _goodbyeMessageBuffer;

    public GreetingHandler(DiscordLogger discordLogger, BotSettingsService botSettingsService, GoodbyeMessageBuffer goodbyeMessageBuffer)
    {
        _discordLogger = discordLogger;
        _botSettingsService = botSettingsService;
        _goodbyeMessageBuffer = goodbyeMessageBuffer;
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
        string memberName = notification.EventArgs.Member.DisplayName;

        _goodbyeMessageBuffer.AddUser(memberName);

        return Task.CompletedTask;
    }
}
