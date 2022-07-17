using MediatR;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers;
public class ClientStatusHandler : INotificationHandler<ClientHeartbeatNotification>,
                                    INotificationHandler<ClientReadyOrResumedNotification>
{
    private readonly BotSettingsService _botSettingsService;
    private readonly MessageRefsService _messageRefsService;
    private readonly DiscordLogger _discordLogger;

    public ClientStatusHandler(BotSettingsService botSettingsService, MessageRefsService messageRefsService, DiscordLogger discordLogger)
    {
        _botSettingsService = botSettingsService;
        _messageRefsService = messageRefsService;
        _discordLogger = discordLogger;
    }

    public Task Handle(ClientHeartbeatNotification notification, CancellationToken cancellationToken)
    {
        return _botSettingsService.SetLastHeartbeat(notification.EventArgs.Timestamp);
    }

    public async Task Handle(ClientReadyOrResumedNotification notification, CancellationToken cancellationToken)
    {
        var lastHeartbeat = await _botSettingsService.GetLastHeartbeat() ?? DateTimeOffset.Now.AddHours(-1);

        await _discordLogger.LogExtendedActivityMessage($"Client ready or resumed. Last heartbeat: {lastHeartbeat}");

        var createdCount = await _messageRefsService.PopulateMessageRefs(lastHeartbeat);

        if (createdCount == 0) return;

        await _discordLogger.LogExtendedActivityMessage($"Populated {createdCount} message refs");
    }
}
