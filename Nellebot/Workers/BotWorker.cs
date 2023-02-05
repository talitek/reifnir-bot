using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.EventHandlers;
using Nellebot.NotificationHandlers;

namespace Nellebot.Workers;

public class BotWorker : IHostedService
{
    private readonly BotOptions _options;
    private readonly ILogger<BotWorker> _logger;
    private readonly DiscordClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly CommandEventHandler _commandEventHandler;
    private readonly AwardEventHandler _awardEventHandler;
    private readonly EventQueueChannel _eventQueue;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        IServiceProvider serviceProvider,
        CommandEventHandler commandEventHandler,
        AwardEventHandler awardEventHandler,
        EventQueueChannel eventQueue)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _commandEventHandler = commandEventHandler;
        _awardEventHandler = awardEventHandler;
        _eventQueue = eventQueue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        string commandPrefix = _options.CommandPrefix;

        CommandsNextExtension commands = _client.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = new[] { commandPrefix },
            Services = _serviceProvider,
            EnableDefaultHelp = false,
        });

        await _client.ConnectAsync();

        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        _client.SocketOpened += OnClientConnected;
        _client.SocketClosed += OnClientDisconnected;
        _client.Ready += OnClientReady;
        _client.Heartbeated += OnClientHeartbeat;
        _client.Resumed += OnClientResumed;

        _commandEventHandler.RegisterHandlers(commands);
        _awardEventHandler.RegisterHandlers();

        RegisterMessageHandlers();
        RegisterGuildEventHandlers();
    }

    private void RegisterMessageHandlers()
    {
        _client.MessageCreated += (sender, args) => _eventQueue.Writer.WriteAsync(new MessageCreatedNotification(args)).AsTask();

        _client.MessageDeleted += (sender, args) => _eventQueue.Writer.WriteAsync(new MessageDeletedNotification(args)).AsTask();

        _client.MessagesBulkDeleted += (sender, args) => _eventQueue.Writer.WriteAsync(new MessageBulkDeletedNotification(args)).AsTask();
    }

    private void RegisterGuildEventHandlers()
    {
        _client.GuildMemberAdded += (sender, args) => _eventQueue.Writer.WriteAsync(new GuildMemberAddedNotification(args)).AsTask();

        _client.GuildMemberRemoved += (sender, args) => _eventQueue.Writer.WriteAsync(new GuildMemberRemovedNotification(args)).AsTask();

        _client.GuildMemberUpdated += (sender, args) => _eventQueue.Writer.WriteAsync(new GuildMemberUpdatedNotification(args)).AsTask();

        _client.GuildBanAdded += (sender, args) => _eventQueue.Writer.WriteAsync(new GuildBanAddedNotification(args)).AsTask();

        _client.GuildBanRemoved += (sender, args) => _eventQueue.Writer.WriteAsync(new GuildBanRemovedNotification(args)).AsTask();
    }

    private Task OnClientHeartbeat(DiscordClient sender, HeartbeatEventArgs e)
    {
        return _eventQueue.Writer.WriteAsync(new ClientHeartbeatNotification(e)).AsTask();
    }

    private Task OnClientDisconnected(DiscordClient sender, SocketCloseEventArgs e)
    {
        return _eventQueue.Writer.WriteAsync(new ClientDisconnected(e)).AsTask();
    }

    private Task OnClientConnected(DiscordClient sender, SocketEventArgs e)
    {
        _logger.LogInformation("Bot connected");

        return Task.CompletedTask;
    }

    private async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        await _eventQueue.Writer.WriteAsync(new ClientReadyOrResumedNotification());

        try
        {
            string commandPrefix = _options.CommandPrefix;

            var activity = new DiscordActivity($"\"{commandPrefix}help\" for help", ActivityType.Playing);

            await _client.UpdateStatusAsync(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnClientReady");
        }
    }

    private Task OnClientResumed(DiscordClient sender, ReadyEventArgs e)
    {
        _logger.LogInformation("Bot resumed");

        return _eventQueue.Writer.WriteAsync(new ClientReadyOrResumedNotification()).AsTask();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        return _client.DisconnectAsync();
    }
}
