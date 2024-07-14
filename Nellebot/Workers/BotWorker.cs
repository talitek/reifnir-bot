using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandModules;
using Nellebot.CommandModules.Messages;

namespace Nellebot.Workers;

public class BotWorker : IHostedService
{
    private readonly DiscordClient _client;
    private readonly CommandEventHandler _commandEventHandler;
    private readonly ILogger<BotWorker> _logger;
    private readonly BotOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        IServiceProvider serviceProvider,
        CommandEventHandler commandEventHandler)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _commandEventHandler = commandEventHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        RegisterClassicCommands();

        ConfigureInteractivity();

        await RegisterNewCommands();

        // TODO Set up the bot's activity here instead of the connected event handler
        await _client.ConnectAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        return _client.DisconnectAsync();
    }

    private void RegisterClassicCommands()
    {
        string commandPrefix = _options.CommandPrefix;

        CommandsNextExtension commands = _client.UseCommandsNext(
            new CommandsNextConfiguration
            {
                StringPrefixes = new[] { commandPrefix },
                EnableDefaultHelp = false,
            });

        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        _commandEventHandler.RegisterHandlers(commands);
    }

    private async Task RegisterNewCommands()
    {
        ulong guildId = _options.GuildId;
        CommandsExtension commands = _client.UseCommands(
            new CommandsConfiguration
            {
                // TODO adapt old error handling to new commands api
                UseDefaultCommandErrorHandler = true,
            });

        commands.AddCommands(typeof(Program).Assembly, guildId);

        string commandPrefix = _options.CommandPrefix;
        var textCommandProcessor = new TextCommandProcessor(
            new TextCommandConfiguration()
            {
                IgnoreBots = true,
                PrefixResolver = new DefaultPrefixResolver(false, commandPrefix).ResolvePrefixAsync,
            });

        await commands.AddProcessorsAsync(textCommandProcessor);

        commands.AddChecks(typeof(Program).Assembly);
    }

    private void ConfigureInteractivity()
    {
        _client.UseInteractivity(
            new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.Ignore,
            });
    }
}
