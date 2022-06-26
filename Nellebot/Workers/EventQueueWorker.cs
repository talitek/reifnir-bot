using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;
using Nellebot.NotificationHandlers;

namespace Nellebot.Workers
{
    public class EventQueue : ConcurrentQueue<EventNotification>
    {

    }

    public class EventQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<EventQueueWorker> _logger;
        private readonly EventQueue _eventQueue;
        private readonly IMediator _mediator;

        public EventQueueWorker(
                ILogger<EventQueueWorker> logger,
                EventQueue eventQueue,
                IMediator mediator
            )
        {
            _logger = logger;
            _eventQueue = eventQueue;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_eventQueue.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _eventQueue.TryDequeue(out var @event);

                    if (@event != null)
                    {
                        _logger.LogDebug($"Dequeued event. {_eventQueue.Count} left in queue");

                        switch (@event)
                        {
                            case GuildMemberUpdatedNotification guildMemberUpdated:
                                await _mediator.Publish(guildMemberUpdated, stoppingToken);
                                break;
                            default:
                                _logger.LogWarning($"Unknown EventNotification type");
                                break;
                        };

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, typeof(CommandQueueWorker).Name);
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
