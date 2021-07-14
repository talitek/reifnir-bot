using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers
{
    public class CommandRequestPipelineBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> _logger;
        private readonly DiscordErrorLogger _discordErrorLogger;

        public CommandRequestPipelineBehaviour(
            ILogger<CommandRequestPipelineBehaviour<TRequest, TResponse>> logger,
            DiscordErrorLogger discordErrorLogger
            )
        {
            _logger = logger;
            _discordErrorLogger = discordErrorLogger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
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

            await _discordErrorLogger.LogDiscordError(ctx, ex.ToString());

            _logger.LogError(ex, nameof(HandeCommandRequestException));
        }
    }
}
