using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.CommandHandlers;
using Nellebot.Services.Loggers;

namespace Nellebot.Infrastructure;

public class CommandRequestPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> _logger;
    private readonly IDiscordErrorLogger _discordErrorLogger;

    public CommandRequestPipelineBehaviour(
        ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> logger,
        IDiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _discordErrorLogger = discordErrorLogger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (request is CommandRequest commandRequest)
            {
                await HandeCommandRequestException(commandRequest, ex);
                return default!;
            }
            else
            {
                throw;
            }
        }
    }

    private async Task HandeCommandRequestException(CommandRequest request, Exception ex)
    {
        var ctx = request.Ctx;

        await ctx.RespondAsync(ex.Message);

        _discordErrorLogger.LogCommandError(ctx, ex.ToString());

        _logger.LogError(ex, nameof(HandeCommandRequestException));
    }
}
