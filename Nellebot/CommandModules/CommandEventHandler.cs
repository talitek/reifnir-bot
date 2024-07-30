using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.EventArgs;
using DSharpPlus.Commands.Exceptions;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Services.Loggers;

namespace Nellebot.CommandModules;

public class CommandEventHandler
{
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly ILogger<CommandEventHandler> _logger;
    private readonly BotOptions _options;

    public CommandEventHandler(
        IOptions<BotOptions> options,
        ILogger<CommandEventHandler> logger,
        IDiscordErrorLogger discordErrorLogger)
    {
        _options = options.Value;
        _logger = logger;
        _discordErrorLogger = discordErrorLogger;
    }

    public void RegisterHandlers(CommandsExtension commands)
    {
        commands.CommandExecuted += OnCommandExecuted;
        commands.CommandErrored += OnCommandErrored;
    }

    private Task OnCommandExecuted(CommandsExtension sender, CommandExecutedEventArgs e)
    {
        CommandContext ctx = e.Context;
        string user = ctx.Member?.DisplayName ?? ctx.User.Username;

        switch (ctx)
        {
            case TextCommandContext textCtx:
                DiscordMessage message = textCtx.Message;

                string messageContent = message.Content;

                _logger.LogDebug("Text command: {user} -> {messageContent}", user, messageContent);
                break;

            case SlashCommandContext slashCtx:
                // TODO implement a ToString method for Command that includes the command and its arguments
                string commandName = slashCtx.Command.FullName;

                _logger.LogDebug("Slash command: {user} -> {commandName}", user, commandName);
                break;

            default:
                _logger.LogCritical("Unknown command context type");
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Try to find a suitable error message to return to the user.
    ///     Log error to discord logger.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    private async Task OnCommandErrored(CommandsExtension sender, CommandErroredEventArgs e)
    {
        CommandContext ctx = e.Context;
        Exception exception = e.Exception;

        var errorMessage = string.Empty;
        var appendHelpText = false;

        string commandPrefix = _options.CommandPrefix;
        var commandHelpText = $"Type \"{commandPrefix}help\" to get some help.";

        string message = ctx is TextCommandContext textCtx ? textCtx.Message.Content : string.Empty;

        bool isChecksFailedException = exception is ChecksFailedException;
        bool isUnknownCommandException = exception is CommandNotFoundException;

        // TODO: If this isn't enough, create a custom exception class for validation errors
        bool isPossiblyValidationException = exception is ArgumentException;

        if (isUnknownCommandException)
        {
            errorMessage = "I do not recognize your command.";
            appendHelpText = true;
        }
        else if (isChecksFailedException)
        {
            var checksFailedException = (ChecksFailedException)exception;

            ContextCheckFailedData? failedCheck = checksFailedException.Errors.FirstOrDefault();

            if (failedCheck is null)
            {
                errorMessage = "An unknown check failed.";
            }
            else
            {
                ContextCheckAttribute contextCheckAttribute = failedCheck.ContextCheckAttribute;

                if (contextCheckAttribute is BaseCommandCheckAttribute)
                {
                    errorMessage = "I do not care for DM commands.";
                }
                else if (contextCheckAttribute is RequirePermissionsAttribute or RequireTrustedMemberAttribute)
                {
                    errorMessage = "You do not have permission to do that.";
                }
                else
                {
                    errorMessage = "Preexecution check failed.";
                }
            }
        }
        else if (isPossiblyValidationException)
        {
            errorMessage = $"{exception.Message}.";
            appendHelpText = true;
        }

        if (string.IsNullOrWhiteSpace(errorMessage))
            errorMessage = "Something went wrong.";

        if (appendHelpText)
            errorMessage += $" {commandHelpText}";

        await ctx.RespondAsync(errorMessage);

        _discordErrorLogger.LogCommandError(ctx, exception.ToString());

        _logger.LogWarning("Message: {message}\r\nCommand failed: {exception})", message, exception);
    }
}
