using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nellebot.Services;

namespace Nellebot.Workers;

public class MessageAwardQueueWorker : BackgroundService
{
    private readonly ILogger<MessageAwardQueueWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly MessageAwardQueueChannel _channel;

    public MessageAwardQueueWorker(
            ILogger<MessageAwardQueueWorker> logger,
            IServiceProvider serviceProvider,
            MessageAwardQueueChannel channel)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _channel = channel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (MessageAwardItem queueItem in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                if (queueItem != null)
                {
                    _logger.LogDebug("Dequeued command. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                    // TODO rewrite to CQRS commands
                    using IServiceScope scope = _serviceProvider.CreateScope();

                    AwardMessageService awardMessageService = scope.ServiceProvider.GetRequiredService<AwardMessageService>();

                    switch (queueItem.Action)
                    {
                        case MessageAwardQueueAction.ReactionChanged:
                            await awardMessageService.HandleAwardChange(queueItem);
                            break;
                        case MessageAwardQueueAction.MessageUpdated:
                            await awardMessageService.HandleAwardMessageUpdated(queueItem);
                            break;
                        case MessageAwardQueueAction.MessageDeleted:
                            await awardMessageService.HandleAwardMessageDeleted(queueItem);
                            break;
                        case MessageAwardQueueAction.AwardDeleted:
                            await awardMessageService.HandleAwardedMessageDeleted(queueItem);
                            break;
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(CommandQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }
}
