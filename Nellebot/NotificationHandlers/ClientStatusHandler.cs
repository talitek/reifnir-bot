using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Common.Extensions;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.NotificationHandlers;

public class ClientStatusHandler : INotificationHandler<ClientHeartbeatNotification>,
    INotificationHandler<SessionCreatedOrResumedNotification>,
    INotificationHandler<ClientDisconnected>
{
    // POC: Make better
    private static bool IsClientActuallyReady;

    private readonly BotSettingsService _botSettingsService;
    private readonly DiscordLogger _discordLogger;
    private readonly ILogger<ClientStatusHandler> _logger;
    private readonly MessageRefsService _messageRefsService;
    private readonly BotOptions _options;

    public ClientStatusHandler(
        BotSettingsService botSettingsService,
        MessageRefsService messageRefsService,
        DiscordLogger discordLogger,
        ILogger<ClientStatusHandler> logger,
        IOptions<BotOptions> options)
    {
        _botSettingsService = botSettingsService;
        _messageRefsService = messageRefsService;
        _discordLogger = discordLogger;
        _logger = logger;
        _options = options.Value;
    }

    public Task Handle(ClientDisconnected notification, CancellationToken cancellationToken)
    {
        IsClientActuallyReady = false;

        _logger.LogInformation($"Bot disconected {notification.EventArgs.CloseMessage}");

        return Task.CompletedTask;
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

    public async Task Handle(SessionCreatedOrResumedNotification notification, CancellationToken cancellationToken)
    {
        var lastHeartbeat = await _botSettingsService.GetLastHeartbeat() ?? DateTimeOffset.UtcNow;

        var timeSinceLastHeartbeat = DateTimeOffset.UtcNow - lastHeartbeat;

        if (timeSinceLastHeartbeat.TotalMinutes > 1)
        {
            var message = $"Client ready or resumed. Last heartbeat: {lastHeartbeat.ToIsoDateTimeString()}.";
            message += $" More than {timeSinceLastHeartbeat.TotalMinutes:0.00} minutes since last heartbeat.";

            _logger.LogDebug(message);

            _discordLogger.LogExtendedActivityMessage(message);
        }

        if (_options.AutoPopulateMessagesOnReadyEnabled)
        {
            var createdCount = await _messageRefsService.PopulateMessageRefs(lastHeartbeat, notification.EventSource);

            if (createdCount > 0)
            {
                _logger.LogDebug($"Populated {createdCount} message refs");
                _discordLogger.LogExtendedActivityMessage($"Populated {createdCount} message refs");
            }
        }

        IsClientActuallyReady = true;
    }
}
