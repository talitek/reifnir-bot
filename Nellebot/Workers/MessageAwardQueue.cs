using System.Collections.Concurrent;
using DSharpPlus.Entities;

namespace Nellebot.Workers
{
    public class MessageAwardQueue : ConcurrentQueue<MessageAwardQueueItem>
    {

    }

    public class MessageAwardQueueItem
    {
        public DiscordMessage DiscordMessage { get; } = null!;
        public MessageAwardQueueAction Action { get; }

        // Used when message is deleted. TODO refactor to different object
        public ulong DiscordMessageId { get; }
        public DiscordChannel? DiscordChannel {get;}

        public MessageAwardQueueItem(DiscordMessage discordMessage, MessageAwardQueueAction action)
        {
            DiscordMessage = discordMessage;
            Action = action;
        }

        public MessageAwardQueueItem(ulong discordMessageId, DiscordChannel channel, MessageAwardQueueAction action)
        {
            DiscordMessageId = discordMessageId;
            DiscordChannel = channel;
            Action = action;
        }
    }

    public enum MessageAwardQueueAction
    {
        ReactionChanged = 0,
        MessageUpdated = 1,
        MessageDeleted = 2,
        AwardDeleted = 3
    }
}
