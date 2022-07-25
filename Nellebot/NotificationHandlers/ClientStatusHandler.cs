using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Text;
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
        _logger.LogTrace($"Heartbeated: {notification.EventArgs.Timestamp.ToIsoDateTimeString()}");

        if (!IsClientActuallyReady)
        {
            _logger.LogDebug("Client not actually ready. Skipping heartbeat save.");
            return Task.CompletedTask;
        }

        return _botSettingsService.SetLastHeartbeat(notification.EventArgs.Timestamp);
    }

    public async Task Handle(ClientReadyOrResumedNotification notification, CancellationToken cancellationToken)
    {
        var lastHeartbeat = await _botSettingsService.GetLastHeartbeat() ?? DateTimeOffset.UtcNow;
        
        var timeSinceLastHeartbeat = DateTimeOffset.UtcNow - lastHeartbeat;

        var message = $"Client ready or resumed. Last heartbeat: {lastHeartbeat.ToIsoDateTimeString()}";
        message += $" which was {timeSinceLastHeartbeat.TotalMinutes:0.00} minutes ago.";

        //if (timeSinceLastHeartbeat.TotalMinutes > 5)
        //    var message = $"Client ready or resumed. Last heartbeat: {lastHeartbeat.ToIsoDateTimeString()}.";
        //    message += $" More than {timeSinceLastHeartbeat.TotalMinutes:0} minutes since last heartbeat";

        _logger.LogDebug(message);

        await _discordLogger.LogExtendedActivityMessage(message.ToString());

        var createdCount = await _messageRefsService.PopulateMessageRefs(lastHeartbeat);

        if (createdCount > 0)
        {
            _logger.LogDebug($"Populated {createdCount} message refs");
            await _discordLogger.LogExtendedActivityMessage($"Populated {createdCount} message refs");
        }

        IsClientActuallyReady = true;
    }

    public Task Handle(ClientDisconnected notification, CancellationToken cancellationToken)
    {
        IsClientActuallyReady = false;

        _logger.LogInformation($"Bot disconected {notification.EventArgs.CloseMessage}");

        return Task.CompletedTask;
    }
}
