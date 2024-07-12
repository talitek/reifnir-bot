using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
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

        RegisterSlashCommands();

        ConfigureInteractivity();

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

    private void RegisterSlashCommands()
    {
        SlashCommandsExtension slashCommands =
            _client.UseSlashCommands(new SlashCommandsConfiguration { Services = _serviceProvider });

        slashCommands.RegisterCommands<GlobalSlashModule>();
        slashCommands.RegisterCommands<ModmailModule>(_options.GuildId);
        slashCommands.RegisterCommands<OrdbokSlashModule>(_options.GuildId);
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
