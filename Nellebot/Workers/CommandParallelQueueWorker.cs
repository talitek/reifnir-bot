using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;

namespace Nellebot.Workers
{
    public class CommandParallelQueue : ConcurrentQueue<CommandRequest>
    {

    }

    public class CommandParallelQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 0;

        private readonly ILogger<CommandQueueWorker> _logger;
        private readonly CommandParallelQueue _commandQueue;
        private readonly IMediator _mediator;

        public CommandParallelQueueWorker(
                ILogger<CommandQueueWorker> logger,
                CommandParallelQueue commandQueue,
                IMediator mediator
            )
        {
            _logger = logger;
            _commandQueue = commandQueue;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_commandQueue.Count == 0 || !_commandQueue.TryDequeue(out var command))
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _logger.LogTrace($"Dequeued (parallel) command. {_commandQueue.Count} left in queue");

                    _ = Task.Run(() => _mediator.Send(command, stoppingToken));

                    nextDelay = BusyDelay;

                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, nameof(CommandQueueWorker));
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
