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

    public AbstractQueueChannel(Channel<T> channel)
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

public record MessageAwardItem
{
    public DiscordMessage DiscordMessage { get; } = null!;

    public MessageAwardQueueAction Action { get; }

    // Used when message is deleted. TODO refactor to different object
    public ulong DiscordMessageId { get; }

    public DiscordChannel? DiscordChannel { get; }

    public MessageAwardItem(DiscordMessage discordMessage, MessageAwardQueueAction action)
    {
        DiscordMessage = discordMessage;
        Action = action;
    }

    public MessageAwardItem(ulong discordMessageId, DiscordChannel channel, MessageAwardQueueAction action)
    {
        DiscordMessageId = discordMessageId;
        DiscordChannel = channel;
        Action = action;
    }
}

public class MessageAwardQueueChannel : AbstractQueueChannel<MessageAwardItem>
{
    public MessageAwardQueueChannel(Channel<MessageAwardItem> channel)
        : base(channel)
    {
    }
}

public enum MessageAwardQueueAction
{
    ReactionChanged = 0,
    MessageUpdated = 1,
    MessageDeleted = 2,
    AwardDeleted = 3,
}
