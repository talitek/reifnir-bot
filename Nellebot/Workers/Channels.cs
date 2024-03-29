using System.Threading.Channels;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.CommandHandlers;

#pragma warning disable SA1402 // File may only contain a single type

namespace Nellebot.Workers;

public interface IQueueChannel<T>
    where T : class
{
    public ChannelWriter<T> Writer { get; }

    public ChannelReader<T> Reader { get; }
}

public abstract class AbstractQueueChannel<T> : IQueueChannel<T>
    where T : class
{
    private readonly Channel<T> _channel;

    protected AbstractQueueChannel(Channel<T> channel)
    {
        _channel = channel;
    }

    public ChannelWriter<T> Writer => _channel.Writer;

    public ChannelReader<T> Reader => _channel.Reader;
}

public class RequestQueueChannel : AbstractQueueChannel<IRequest>
{
    public RequestQueueChannel(Channel<IRequest> channel)
        : base(channel)
    {
    }
}

public class CommandQueueChannel : AbstractQueueChannel<ICommand>
{
    public CommandQueueChannel(Channel<ICommand> channel)
        : base(channel)
    {
    }
}

public class CommandParallelQueueChannel : AbstractQueueChannel<ICommand>
{
    public CommandParallelQueueChannel(Channel<ICommand> channel)
        : base(channel)
    {
    }
}

public class EventQueueChannel : AbstractQueueChannel<INotification>
{
    public EventQueueChannel(Channel<INotification> channel)
        : base(channel)
    {
    }
}

public record BaseDiscordLogItem(ulong DiscordGuildId, ulong DiscordChannelId);

public record DiscordLogItem<T>(T Message, ulong DiscordGuildId, ulong DiscordChannelId)
    : BaseDiscordLogItem(DiscordGuildId, DiscordChannelId);

public class DiscordLogChannel : AbstractQueueChannel<BaseDiscordLogItem>
{
    public DiscordLogChannel(Channel<BaseDiscordLogItem> channel)
        : base(channel)
    {
    }
}
