using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Common.Extensions;
using Nellebot.Helpers;
using Nellebot.Utils;

namespace Nellebot.NotificationHandlers;

public class SuggestionHandlerV2 : INotificationHandler<MessageCreatedNotification>
{
    private readonly BotOptions _options;

    public SuggestionHandlerV2(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        var suggestionsForumChannelId = _options.SuggestionsChannelId2;

        var channelParentId = notification.EventArgs.Channel?.Parent.Id;

        if (channelParentId == null || channelParentId != suggestionsForumChannelId) return;

        var message = notification.EventArgs.Message;

        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowUp));
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowDown));
        await message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.ArrowUpDown));
    }
}
