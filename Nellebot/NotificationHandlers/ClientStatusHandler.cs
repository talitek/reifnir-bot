using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers;
public class ClientStatusHandler : INotificationHandler<ClientHeartbeatNotification>,
                                    INotificationHandler<ClientReadyOrResumedNotification>,
                                    INotificationHandler<ClientDisconnected>
{
    // POC: Make better
    public static bool IsClientActuallyReady = false;

    private readonly BotSettingsService _botSettingsService;
    private readonly MessageRefsService _messageRefsService;
    private readonly DiscordLogger _discordLogger;
    private readonly ILogger<ClientStatusHandler> _logger;

    public ClientStatusHandler(BotSettingsService botSettingsService, MessageRefsService messageRefsService, DiscordLogger discordLogger, ILogger<ClientStatusHandler> logger)
    {
        _botSettingsService = botSettingsService;
        _messageRefsService = messageRefsService;
        _discordLogger = discordLogger;
        _logger = logger;
    }

    public Task Handle(ClientHeartbeatNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Heartbeated: {notification.EventArgs.Timestamp.ToIsoDateTimeString()}");

        if (!IsClientActuallyReady)
        {
            _logger.LogDebug("Client not actually ready. Skipping heartbeat save.");
            return Task.CompletedTask; ;
        }

        return _botSettingsService.SetLastHeartbeat(notification.EventArgs.Timestamp);
    }

    public async Task Handle(ClientReadyOrResumedNotification notification, CancellationToken cancellationToken)
    {
        var lastHeartbeat = await _botSettingsService.GetLastHeartbeat() ?? DateTimeOffset.UtcNow;

        _logger.LogDebug($"Client ready or resumed. Last heartbeat: {lastHeartbeat.ToIsoDateTimeString()}");

        var createdCount = await _messageRefsService.PopulateMessageRefs(lastHeartbeat);

        if (createdCount > 0)
            await _discordLogger.LogExtendedActivityMessage($"Populated {createdCount} message refs");

        await _discordLogger.LogExtendedActivityMessage($"Client ready or resumed. Last heartbeat: {lastHeartbeat.ToIsoDateTimeString()}.");

        IsClientActuallyReady = true;
    }

    public Task Handle(ClientDisconnected notification, CancellationToken cancellationToken)
    {
        IsClientActuallyReady = false;

        _logger.LogInformation($"Bot disconected {notification.EventArgs.CloseMessage}");

        return _discordLogger.LogExtendedActivityMessage($"Client disconnected.");
    }
}
