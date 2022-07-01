using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class SuggestionHandler : INotificationHandler<MessageCreatedNotification>
    {
        private readonly BotOptions _options;

        public SuggestionHandler(IOptions<BotOptions> options)
        {
            _options = options.Value;
        }

        public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
        {
            if (notification.EventArgs.Channel.Id != _options.SuggestionsChannelId) return;

            var message = notification.EventArgs.Message;

            await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowUp));
            await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowDown));
            await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowUpDown));
        }
    }
}
