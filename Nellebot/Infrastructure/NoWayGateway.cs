using System;
using System.Threading.Tasks;
using DSharpPlus.Clients;
using DSharpPlus.Net.Gateway;
using Nellebot.NotificationHandlers;
using Nellebot.Workers;

namespace Nellebot.Infrastructure;

// ReSharper disable once ClassNeverInstantiated.Global
public class NoWayGateway : IGatewayController
{
    private readonly EventQueueChannel _eventQueue;

    public NoWayGateway(EventQueueChannel eventQueue)
    {
        _eventQueue = eventQueue;
    }

    public ValueTask ZombiedAsync(IGatewayClient client)
    {
        return ValueTask.CompletedTask;
    }

    public async Task HeartbeatedAsync(IGatewayClient client)
    {
        DateTime heartbeatTimestamp = DateTime.UtcNow;
        TimeSpan ping = client.Ping;

        await _eventQueue.Writer.WriteAsync(new ClientHeartbeatNotification(heartbeatTimestamp, ping));
    }
}
