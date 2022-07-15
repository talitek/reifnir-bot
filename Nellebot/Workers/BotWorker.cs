using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.EventHandlers;
using Nellebot.NotificationHandlers;
using Nellebot.Workers;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot
{
    public class BotWorker : IHostedService
    {
        private readonly BotOptions _options;
        private readonly ILogger<BotWorker> _logger;
        private readonly DiscordClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly CommandEventHandler _commandEventHandler;
        private readonly AwardEventHandler _awardEventHandler;
        private readonly EventQueue _eventQueue;

        public BotWorker(
            IOptions<BotOptions> options,
            ILogger<BotWorker> logger,
            DiscordClient client,
            IServiceProvider serviceProvider,
            CommandEventHandler commandEventHandler,
            AwardEventHandler awardEventHandler,
            EventQueue eventQueue
            )
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

            var commandPrefix = _options.CommandPrefix;

            var commands = _client.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { commandPrefix },
                Services = _serviceProvider,
                EnableDefaultHelp = false
            });

            await _client.ConnectAsync();

            commands.RegisterCommands(Assembly.GetExecutingAssembly());

            _client.SocketOpened += OnClientConnected;
            _client.SocketClosed += OnClientDisconnected;
            _client.Ready += OnClientReady;

            _commandEventHandler.RegisterHandlers(commands);
            _awardEventHandler.RegisterHandlers();

            RegisterMessageHandlers();
            RegisterGuildEventHandlers();
        }

        private void RegisterMessageHandlers()
        {
            _client.MessageCreated += (sender, args) =>
            {
                _eventQueue.Enqueue(new MessageCreatedNotification(args));
                return Task.CompletedTask;
            };

            _client.MessageDeleted += (sender, args) =>
            {
                _eventQueue.Enqueue(new MessageDeletedNotification(args));
                return Task.CompletedTask;
            };

            _client.MessagesBulkDeleted += (sender, args) =>
            {
                _eventQueue.Enqueue(new MessageBulkDeletedNotification(args));
                return Task.CompletedTask;
            };
        }

        private void RegisterGuildEventHandlers()
        {
            _client.GuildMemberAdded += (sender, args) =>
            {
                _eventQueue.Enqueue(new GuildMemberAddedNotification(args));
                return Task.CompletedTask;
            };

            _client.GuildMemberRemoved += (sender, args) =>
            {
                _eventQueue.Enqueue(new GuildMemberRemovedNotification(args));
                return Task.CompletedTask;
            };

            _client.GuildMemberUpdated += (sender, args) =>
            {
                _eventQueue.Enqueue(new GuildMemberUpdatedNotification(args));
                return Task.CompletedTask;
            };

            _client.GuildBanAdded += (sender, args) =>
            {
                _eventQueue.Enqueue(new GuildBanAddedNotification(args));
                return Task.CompletedTask;
            };

            _client.GuildBanRemoved += (sender, args) =>
            {
                _eventQueue.Enqueue(new GuildBanRemovedNotification(args));
                return Task.CompletedTask;
            };

            _client.PresenceUpdated += (sender, args) =>
            {
                _eventQueue.Enqueue(new PresenceUpdatedNotification(args));
                return Task.CompletedTask;
            };
        }

        private Task OnClientDisconnected(DiscordClient sender, SocketCloseEventArgs e)
        {
            _logger.LogInformation($"Bot disconected {e.CloseMessage}");

            return Task.CompletedTask;
        }

        private Task OnClientConnected(DiscordClient sender, SocketEventArgs e)
        {
            _logger.LogInformation("Bot connected");

            return Task.CompletedTask;
        }

        private async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            _logger.LogInformation("Bot ready");

            try
            {
                var commandPrefix = _options.CommandPrefix;

                var activity = new DiscordActivity($"\"{commandPrefix}help\" for help", ActivityType.Playing);

                await _client.UpdateStatusAsync(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnClientReady");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping bot");

            await _client.DisconnectAsync();
        }
    }
}
