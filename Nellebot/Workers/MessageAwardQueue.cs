using System.Collections.Concurrent;
using DSharpPlus.Entities;

namespace Nellebot.Workers
{
    public class MessageAwardQueue : ConcurrentQueue<MessageAwardQueueItem>
    {

    }

    public class MessageAwardQueueItem
    {
        public DiscordMessage DiscordMessage { get; }

        public MessageAwardQueueItem(DiscordMessage discordMessage)
        {
            DiscordMessage = discordMessage;
        }
    }
}
