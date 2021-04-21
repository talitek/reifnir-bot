using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Data.Repositories;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.Workers
{
    public class MessageAwardQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<MessageAwardQueueWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly MessageAwardQueue _awardQueue;

        public MessageAwardQueueWorker(
                ILogger<MessageAwardQueueWorker> logger,
                IServiceProvider serviceProvider,
                DiscordErrorLogger discordErrorLogger,
                MessageAwardQueue awardQueue
            )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _discordErrorLogger = discordErrorLogger;
            _awardQueue = awardQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_awardQueue.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _awardQueue.TryDequeue(out var awardMessageQueueItem);

                    if (awardMessageQueueItem != null)
                    {
                        _logger.LogDebug($"Dequeued message. {_awardQueue.Count} left in queue");

                        using var scope = _serviceProvider.CreateScope();

                        var awardMessageService = scope.ServiceProvider.GetRequiredService<AwardMessageService>();

                        switch(awardMessageQueueItem.Action)
                        {
                            case MessageAwardQueueAction.ReactionChanged:
                                await awardMessageService.HandleAwardChange(awardMessageQueueItem);
                                break;
                            case MessageAwardQueueAction.MessageUpdated:
                                await awardMessageService.HandleAwardMessageUpdated(awardMessageQueueItem);
                                break;
                            case MessageAwardQueueAction.MessageDeleted:
                                await awardMessageService.HandleAwardMessageDeleted(awardMessageQueueItem);
                                break;
                        }

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, "MessageAwardQueueWorker");

                        var escapedError = DiscordErrorLogger.ReplaceTicks(ex.ToString());
                        await _discordErrorLogger.LogDiscordError($"`{escapedError}`");
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }


    }
}
