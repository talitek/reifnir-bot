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
using Nellebot.CommandModules.Roles;
using Nellebot.NotificationHandlers;

namespace Nellebot.Workers;

public class BotWorker : IHostedService
{
    private readonly DiscordClient _client;
    private readonly CommandEventHandler _commandEventHandler;
    private readonly EventQueueChannel _eventQueue;
    private readonly ILogger<BotWorker> _logger;
    private readonly BotOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        IServiceProvider serviceProvider,
        CommandEventHandler commandEventHandler,
        EventQueueChannel eventQueue)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _commandEventHandler = commandEventHandler;
        _eventQueue = eventQueue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        RegisterClassicCommands();

        RegisterSlashCommands();

        ConfigureInteractivity();

        RegisterLifecycleEventHandlers();

        RegisterMessageHandlers();
        RegisterGuildEventHandlers();

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
                Services = _serviceProvider,
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
        slashCommands.RegisterCommands<RoleModule>(_options.GuildId);
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

    private void RegisterMessageHandlers()
    {
        _client.MessageReactionAdded += (_, args) =>
            _eventQueue.Writer.WriteAsync(new MessageReactionAddedNotification(args)).AsTask();
        _client.MessageReactionRemoved += (_, args) =>
            _eventQueue.Writer.WriteAsync(new MessageReactionRemovedNotification(args)).AsTask();
        _client.MessageCreated += (_, args) =>
            _eventQueue.Writer.WriteAsync(new MessageCreatedNotification(args)).AsTask();
        _client.MessageUpdated += (_, args) =>
            _eventQueue.Writer.WriteAsync(new MessageUpdatedNotification(args)).AsTask();
        _client.MessageDeleted += (_, args) =>
            _eventQueue.Writer.WriteAsync(new MessageDeletedNotification(args)).AsTask();
        _client.MessagesBulkDeleted += (_, args) =>
            _eventQueue.Writer.WriteAsync(new MessageBulkDeletedNotification(args)).AsTask();
    }

    private void RegisterGuildEventHandlers()
    {
        _client.GuildMemberAdded += (_, args) =>
            _eventQueue.Writer.WriteAsync(new GuildMemberAddedNotification(args)).AsTask();
        _client.GuildMemberRemoved += (_, args) =>
            _eventQueue.Writer.WriteAsync(new GuildMemberRemovedNotification(args)).AsTask();
        _client.GuildMemberUpdated += (_, args) =>
            _eventQueue.Writer.WriteAsync(new GuildMemberUpdatedNotification(args)).AsTask();
        _client.GuildBanAdded += (_, args) =>
            _eventQueue.Writer.WriteAsync(new GuildBanAddedNotification(args)).AsTask();
        _client.GuildBanRemoved += (_, args) =>
            _eventQueue.Writer.WriteAsync(new GuildBanRemovedNotification(args)).AsTask();
    }

    private void RegisterLifecycleEventHandlers()
    {
        _client.SocketOpened += OnClientConnected;
        _client.SocketClosed += OnClientDisconnected;
        _client.SessionCreated += OnSessionCreated;
        _client.SessionResumed += OnSessionResumed;
        _client.Heartbeated += OnClientHeartbeat;
        _client.GuildDownloadCompleted += OnGuildDownloadCompleted;
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

    private async Task OnSessionCreated(DiscordClient sender, SessionReadyEventArgs e)
    {
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

    private Task OnSessionResumed(DiscordClient sender, SessionReadyEventArgs e)
    {
        _logger.LogInformation("Bot resumed");

        return _eventQueue.Writer.WriteAsync(new SessionCreatedOrResumedNotification(nameof(OnSessionResumed)))
            .AsTask();
    }

    private Task OnGuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs args)
    {
        return _eventQueue.Writer.WriteAsync(new SessionCreatedOrResumedNotification(nameof(OnGuildDownloadCompleted)))
            .AsTask();
    }
}
