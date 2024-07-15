using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.CommandModules;

namespace Nellebot.Workers;

public class BotWorker : IHostedService
{
    private readonly DiscordClient _client;
    private readonly CommandEventHandler _commandEventHandler;
    private readonly ILogger<BotWorker> _logger;
    private readonly BotOptions _options;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        CommandEventHandler commandEventHandler)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _commandEventHandler = commandEventHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        ConfigureInteractivity();

        await RegisterCommands();

        await ConnectToGateway();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        return _client.DisconnectAsync();
    }

    private async Task ConnectToGateway()
    {
        string commandPrefix = _options.CommandPrefix;

        var activity = new DiscordActivity($"\"{commandPrefix}help\" for help", DiscordActivityType.Playing);

        await _client.ConnectAsync(activity);
    }

    private async Task RegisterCommands()
    {
        ulong guildId = _options.GuildId;

        CommandsExtension commands = _client.UseCommands(
            new CommandsConfiguration
            {
                UseDefaultCommandErrorHandler = false,
            });

        _commandEventHandler.RegisterHandlers(commands);

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
