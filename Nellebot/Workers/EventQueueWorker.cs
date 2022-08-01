using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.NotificationHandlers;
using Nellebot.Services;
using Nellebot.Services.Loggers;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.Workers
{
    public class EventQueue : ConcurrentQueue<INotification> { }

    public class EventQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 0;

        private readonly ILogger<EventQueueWorker> _logger;
        private readonly EventQueue _eventQueue;
        private readonly NotificationPublisher _publisher;
        private readonly IDiscordErrorLogger _discordErrorLogger;

        public EventQueueWorker(
            ILogger<EventQueueWorker> logger,
            EventQueue eventQueue,
            NotificationPublisher publisher,
            IDiscordErrorLogger discordErrorLogger)
        {
            _logger = logger;
            _eventQueue = eventQueue;
            _publisher = publisher;
            _discordErrorLogger = discordErrorLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                INotification? @event = null;

                try
                {
                    if (_eventQueue.Count == 0 || !_eventQueue.TryDequeue(out @event))
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _logger.LogTrace($"Dequeued event. {_eventQueue.Count} left in queue");

                    await _publisher.Publish(@event, stoppingToken);

                    nextDelay = BusyDelay;
                }
                catch (AggregateException ex)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (@event != null && @event is EventNotification notification)
                        {
                            if (notification.Ctx != null)
                                await _discordErrorLogger.LogEventError(notification.Ctx, innerEx.ToString());
                            else
                                await _discordErrorLogger.LogError(innerEx, nameof(EventQueueWorker));
                        }

                        _logger.LogError(innerEx, nameof(EventQueueWorker));
                    }
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                    {
                        _logger.LogError(ex, nameof(EventQueueWorker));
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
