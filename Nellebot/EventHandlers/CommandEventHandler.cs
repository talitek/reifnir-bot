using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Nellebot.Helpers;
using Nellebot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Nellebot.Attributes;

namespace Nellebot.EventHandlers
{
    public class CommandEventHandler
    {
        private readonly BotOptions _options;
        private readonly ILogger<CommandEventHandler> _logger;
        private readonly DiscordErrorLogger _discordErrorLogger;

        public CommandEventHandler(
            IOptions<BotOptions> options,
            ILogger<CommandEventHandler> logger,
            DiscordErrorLogger discordErrorLogger
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

        private async Task OnCommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            var ctx = e.Context;
            var message = ctx.Message;

            var commandPrefix = _options.CommandPrefix;
            var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

            // Try to find a suitable error message to return to the user
            string errorMessage = string.Empty;

            // Flag unknown commands and not return any error message in this case
            // as it's easy for users to accidentally trigger commands using the prefix
            bool isUnknownCommand = false;
            bool appendHelpText = false;

            const string unknownSubcommandErrorString = "No matching subcommands were found, and this group is not executable.";

            var isChecksFailedException = e.Exception is ChecksFailedException;

            var isUnknownCommandException = e.Exception is CommandNotFoundException;
            var isUnknownSubcommandException = e.Exception.Message == unknownSubcommandErrorString;

            var isCommandConfigException = e.Exception is DuplicateCommandException
                                    || e.Exception is DuplicateOverloadException
                                    || e.Exception is InvalidOverloadException;

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
            else if (isCommandConfigException)
            {
                errorMessage = $"Something's not quite right.";
                appendHelpText = true;
            }
            else if (isChecksFailedException)
            {
                var checksFailedException = (ChecksFailedException)e.Exception;

                var failedCheck = checksFailedException.FailedChecks[0];

                if (failedCheck is BaseCommandCheck)
                {
                    errorMessage = "I do not care for Bot commands or DM commands";
                }
                else if (failedCheck is RequireOwnerOrAdmin)
                {
                    errorMessage = "You do not have permission to do that";
                }
                else
                {
                    errorMessage = "Preexecution check failed";
                }
            }
            //else
            //{
            //    errorMessage = $"Hmm. Your command suffers from a case of **{exceptionMessage}**";
            //}

            if (!isUnknownCommand)
            {
                if (string.IsNullOrWhiteSpace(errorMessage))
                {
                    errorMessage = "Something went wrong.";
                }

                if(appendHelpText)
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
                && !isChecksFailedException;

            if (shouldLogDiscordError)
            {
                var escapedError = DiscordErrorLogger.ReplaceTicks(e.Exception.ToString());
                var escapedMessage = DiscordErrorLogger.ReplaceTicks(message.Content);
                await _discordErrorLogger.LogDiscordError($"Message: `{escapedMessage}`\r\nCommand failed: `{escapedError}`)");
            }

            _logger.LogWarning($"Message: {message.Content}\r\nCommand failed: {e.Exception})");
        }
    }
}