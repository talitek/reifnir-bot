using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nellebot.Workers;

public class BotWorker : IHostedService
{
    private readonly DiscordClient _client;
    private readonly ILogger<BotWorker> _logger;
    private readonly BotOptions _options;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        ConfigureInteractivity();

        await RegisterCommands();

        // TODO Set up the bot's activity here instead of the connected event handler
        await _client.ConnectAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        return _client.DisconnectAsync();
    }

    private async Task RegisterCommands()
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
