using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Services;

namespace Nellebot.Workers;

public class ModmailCleanupWorker : BackgroundService
{
    private const int Delay = 1000 * 60;

    private readonly ILogger<RequestQueueWorker> _logger;
    private readonly IMediator _mediator;
    private readonly ModmailTicketPool _ticketPool;
    private readonly BotOptions _options;

    public ModmailCleanupWorker(ILogger<RequestQueueWorker> logger, IMediator mediator, ModmailTicketPool ticketPool, IOptions<BotOptions> options)
    {
        _logger = logger;
        _mediator = mediator;
        _ticketPool = ticketPool;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var cleanupInterval = TimeSpan.FromHours(_options.ModmailTicketInactiveThresholdInHours);

            while (!stoppingToken.IsCancellationRequested)
            {
                var expiredTickets = _ticketPool.RemoveInactiveTickets(cleanupInterval);

                foreach (var ticket in expiredTickets)
                {
                    await _mediator.Send(new CloseInactiveModmailTicketCommand(ticket), stoppingToken);
                }

                await Task.Delay(Delay, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(ModmailCleanupWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }
}
