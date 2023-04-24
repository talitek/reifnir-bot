using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Nellebot.Attributes;
using Nellebot.Services.Loggers;

namespace Nellebot.EventHandlers
{
    public class CommandEventHandler
    {
        private readonly BotOptions _options;
        private readonly ILogger<CommandEventHandler> _logger;
        private readonly IDiscordErrorLogger _discordErrorLogger;

        public CommandEventHandler(
            IOptions<BotOptions> options,
            ILogger<CommandEventHandler> logger,
            IDiscordErrorLogger discordErrorLogger
            )
        {
            _options = options.Value;
            _logger = logger;
            _discordErrorLogger = discordErrorLogger;
        }

        public void RegisterHandlers(CommandsNextExtension commands)
        {
            commands.CommandExecuted += OnCommandExecuted;
            commands.CommandErrored += OnCommandErrored;
        }

        private Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            var message = e.Context.Message;

            var messageContent = message.Content;
            var username = message.Author.Username;

            _logger.LogDebug($"Command: {username} -> {messageContent}");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Try to find a suitable error message to return to the user.
        /// Log error to discord logger
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            var ctx = e.Context;
            var message = ctx.Message;
            var exception = e.Exception;

            var commandPrefix = _options.CommandPrefix;
            var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

            string errorMessage = string.Empty;

            // Flag unknown commands and not return any error message in this case
            // as it's easy for users to accidentally trigger commands using the prefix
            bool isUnknownCommand = false;
            bool appendHelpText = false;

            const string unknownSubcommandErrorString = "No matching subcommands were found, and this group is not executable.";
            const string unknownOverloadErrorString = "Could not find a suitable overload for the command.";

            var isChecksFailedException = exception is ChecksFailedException;

            var isUnknownCommandException = exception is CommandNotFoundException;
            var isUnknownSubcommandException = exception.Message == unknownSubcommandErrorString;
            var isUnknownOverloadException = exception.Message == unknownOverloadErrorString;

            var isCommandConfigException = exception is DuplicateCommandException
                                    || exception is DuplicateOverloadException
                                    || exception is InvalidOverloadException;

            // TODO: If this isn't enough, create a custom exception class for validation errors
            var isPossiblyValidationException = exception is ArgumentException;

            if (isUnknownCommandException)
            {
                errorMessage = $"I do not recognize your command.";
                isUnknownCommand = true;
                appendHelpText = true;
            }
            else if (isUnknownSubcommandException)
            {
                errorMessage = $"I do not recognize your command.";
                appendHelpText = true;
            }
            else if (isUnknownOverloadException)
            {
                errorMessage = $"Command arguments are (probably) incorrect.";
                appendHelpText = true;
            }
            else if (isCommandConfigException)
            {
                errorMessage = $"Something's not quite right.";
                appendHelpText = true;
            }
            else if (isChecksFailedException)
            {
                var checksFailedException = (ChecksFailedException)exception;

                var failedCheck = checksFailedException.FailedChecks[0];

                if (failedCheck is BaseCommandCheck)
                {
                    errorMessage = "I do not care for DM commands.";
                }
                else if (failedCheck is RequireOwnerOrAdmin || failedCheck is RequireTrustedMember)
                {
                    errorMessage = "You do not have permission to do that.";
                }
                else
                {
                    errorMessage = "Preexecution check failed.";
                }
            }
            else if (isPossiblyValidationException)
            {
                errorMessage = $"{exception.Message}.";
                appendHelpText = true;
            }

            if (!isUnknownCommand)
            {
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = "Something went wrong.";
                }

                if (appendHelpText)
                {
                    errorMessage += $" {commandHelpText}";
                }

                await message.RespondAsync(errorMessage);
            }

            // Log any unhandled exception
            var shouldLogDiscordError =
                   !isUnknownCommandException
                && !isUnknownSubcommandException
                && !isCommandConfigException
                && !isChecksFailedException
                && !isPossiblyValidationException;

            if (shouldLogDiscordError)
            {
                _discordErrorLogger.LogCommandError(ctx, exception.ToString());
            }

            _logger.LogWarning($"Message: {message.Content}\r\nCommand failed: {exception})");
        }
    }
}
