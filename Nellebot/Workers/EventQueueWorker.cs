using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.Infrastructure;
using Nellebot.NotificationHandlers;
using Nellebot.Services.Loggers;

namespace Nellebot.Workers;

public class EventQueueWorker : BackgroundService
{
    private readonly EventQueueChannel _channel;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly ILogger<EventQueueWorker> _logger;
    private readonly NotificationPublisher _publisher;

    public EventQueueWorker(
        ILogger<EventQueueWorker> logger,
        EventQueueChannel channel,
        NotificationPublisher publisher,
        IDiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _channel = channel;
        _publisher = publisher;
        _discordErrorLogger = discordErrorLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                var @event = notification;

                if (@event == null) continue;

                _logger.LogDebug("Dequeued event. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                try
                {
                    await _publisher.Publish(@event, stoppingToken);
                }
                catch (AggregateException ex)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (@event is not null and EventNotification eventNotification)
                        {
                            // TODO extract relevant information from notification object
                            // and pass to LogEventError
                            _discordErrorLogger.LogError(innerEx, nameof(EventQueueWorker));
                        }

                        _logger.LogError(innerEx, nameof(EventQueueWorker));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, nameof(EventQueueWorker));
                    _discordErrorLogger.LogError(ex.Message);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(EventQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(EventQueueWorker));
        }
    }
}
