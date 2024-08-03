using System;
using System.Threading.Tasks;
using DSharpPlus.Clients;
using DSharpPlus.Net.Gateway;
using Nellebot.NotificationHandlers;
using Nellebot.Services.Loggers;
using Nellebot.Workers;

namespace Nellebot.Infrastructure;

// ReSharper disable once ClassNeverInstantiated.Global
public class NoWayGateway : IGatewayController
{
    private readonly EventQueueChannel _eventQueue;
    private readonly DiscordLogger _discordLogger;

    public NoWayGateway(EventQueueChannel eventQueue, DiscordLogger discordLogger)
    {
        _eventQueue = eventQueue;
        _discordLogger = discordLogger;
    }

    public async ValueTask ZombiedAsync(IGatewayClient client)
    {
        _discordLogger.LogExtendedActivityMessage("Connection to gateway zombied. Reconnecting...");
        await client.ReconnectAsync();
    }

    public async Task HeartbeatedAsync(IGatewayClient client)
    {
        DateTime heartbeatTimestamp = DateTime.UtcNow;
        TimeSpan ping = client.Ping;

        await _eventQueue.Writer.WriteAsync(new ClientHeartbeatNotification(heartbeatTimestamp, ping));
    }
}
