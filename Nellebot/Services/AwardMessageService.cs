using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Data.Repositories;
using Nellebot.Helpers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.Services;

public class AwardMessageService
{
    private const string SpoilerImageAttachmentPrefix = "SPOILER_";
    private const string InvisibleChar = "‎ ";
    private readonly AwardMessageRepository _awardMessageRepo;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly DiscordResolver _discordResolver;
    private readonly ILogger<AwardMessageService> _logger;
    private readonly BotOptions _options;

    public AwardMessageService(
        AwardMessageRepository awardMessageRepo,
        ILogger<AwardMessageService> logger,
        IOptions<BotOptions> options,
        IDiscordErrorLogger discordErrorLogger,
        DiscordResolver discordResolver)
    {
        _awardMessageRepo = awardMessageRepo;
        _logger = logger;
        _discordErrorLogger = discordErrorLogger;
        _discordResolver = discordResolver;
        _options = options.Value;
    }

    public async Task HandleAwardChange(MessageAwardItem awardItem)
    {
        // TODO optimize by resolving full message only if it will be posted
        var partialMessage = awardItem.DiscordMessage;

        var message = await _discordResolver.ResolveMessage(partialMessage.Channel, partialMessage.Id);

        if (message == null)
        {
            _logger.LogDebug("Could not resolve message");
            return;
        }

        var channel = message.Channel;
        var guild = channel.Guild;

        var messageAuthor = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

        if (messageAuthor is null)
        {
            _logger.LogDebug("Could not resolve message author");
            return;
        }

        var awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        var awardReactionCount = await GetAwardReactionCount(message, messageAuthor);

        var hasEnoughAwards = awardReactionCount >= _options.RequiredAwardCount;

        var awardMessage = await _awardMessageRepo.GetAwardMessageByOriginalMessageId(awardChannel.Id, message.Id);

        _logger.LogDebug($"Message has {awardReactionCount} awards");

        if (awardMessage == null)
        {
            _logger.LogDebug($"Message ({message.Id}) does not exist in the database");

            if (!hasEnoughAwards)
            {
                _logger.LogDebug($"Not enough awards. {awardReactionCount} / {_options.RequiredAwardCount}");
                return;
            }

            var postedMessage = await PostAwardedMessage(awardChannel, message, messageAuthor, awardReactionCount);

            await _awardMessageRepo.CreateAwardMessage(
                                                       message.Id,
                                                       message.ChannelId,
                                                       postedMessage.Id,
                                                       awardChannel.Id,
                                                       messageAuthor.Id,
                                                       awardReactionCount);
        }
        else
        {
            _logger.LogDebug($"Message ({message.Id}) exists in award channel");

            // TODO keep track if message was removed from award channels
            // so it's handled gracefully i.e. not throw an error
            // when it tries to update a removed message
            await UpdateAwardedMessageText(awardChannel, awardMessage.AwardedMessageId, awardReactionCount);

            await _awardMessageRepo.UpdateAwardCount(awardMessage.Id, awardReactionCount);
        }
    }

    public async Task HandleAwardMessageUpdated(MessageAwardItem awardItem)
    {
        var partialMessage = awardItem.DiscordMessage;

        var message = await _discordResolver.ResolveMessage(partialMessage.Channel, partialMessage.Id);

        if (message == null)
        {
            _logger.LogDebug("Could not resolve message");
            return;
        }

        var channel = message.Channel;
        var guild = channel.Guild;

        var messageAuthor = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

        if (messageAuthor is null)
        {
            _logger.LogDebug("Could not resolve message author");
            return;
        }

        var awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        var awardReactionCount = await GetAwardReactionCount(message, messageAuthor);

        var hasAwards = awardReactionCount > 0;

        if (!hasAwards)
        {
            _logger.LogDebug("Updated message has no awards");
            return;
        }

        var awardMessage = await _awardMessageRepo.GetAwardMessageByOriginalMessageId(awardChannel.Id, message.Id);

        if (awardMessage == null)
        {
            _logger.LogDebug($"Could not find ({message.Id}) in the database");
            return;
        }

        await UpdateAwardedMessageEmbed(awardChannel, awardMessage.AwardedMessageId, message, messageAuthor);
    }

    public async Task HandleAwardMessageDeleted(MessageAwardItem awardItem)
    {
        var messageId = awardItem.DiscordMessageId;

        var awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        var awardMessage = await _awardMessageRepo.GetAwardMessageByOriginalMessageId(awardChannel.Id, messageId);

        if (awardMessage == null)
        {
            _logger.LogDebug($"Message ({messageId}) does not exist in award channel");
            return;
        }

        await DeleteAwardedMessage(awardChannel, awardMessage.AwardedMessageId);

        await _awardMessageRepo.DeleteAwardMessage(awardMessage.Id);
    }

    public async Task HandleAwardedMessageDeleted(MessageAwardItem awardItem)
    {
        var messageId = awardItem.DiscordMessageId;

        var channel = awardItem.DiscordChannel!;

        var awardChannel = await _discordResolver.ResolveChannelAsync(_options.AwardChannelId);

        if (awardChannel is null)
        {
            _logger.LogDebug("Could not resolve awards channel");
            return;
        }

        var awardMessage = await _awardMessageRepo.GetAwardMessageByAwardedMessageId(awardChannel.Id, messageId);

        if (awardMessage is null)
        {
            _logger.LogDebug($"Message ({messageId}) does not exist in award channel");
            return;
        }

        await _awardMessageRepo.DeleteAwardMessage(awardMessage.Id);
    }

    private async Task<DiscordMessage> PostAwardedMessage(
        DiscordChannel awardChannel,
        DiscordMessage originalMessage,
        DiscordMember author,
        uint awardCount)
    {
        var messageChannel = originalMessage.Channel ?? throw new Exception("Channel was null");

        var embed = BuildAwardedMessageEmbed(originalMessage, author, messageChannel.Name);

        var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";

        var messageText = $"{awardText}";

        return await awardChannel.SendMessageAsync(messageText, embed);
    }

    private async Task UpdateAwardedMessageText(DiscordChannel awardChannel, ulong awardedMessageId, uint awardCount)
    {
        var awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

        if (awardedMessage == null)
        {
            return;
        }

        var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";

        var messageText = $"{awardText}";

        await awardedMessage.ModifyAsync(messageText);
    }

    private async Task UpdateAwardedMessageEmbed(
        DiscordChannel awardChannel,
        ulong awardedMessageId,
        DiscordMessage originalMessage,
        DiscordMember author)
    {
        var messageChannel = originalMessage.Channel ?? throw new Exception("Channel was null");

        var awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

        if (awardedMessage == null)
        {
            return;
        }

        var embed = BuildAwardedMessageEmbed(originalMessage, author, messageChannel.Name);

        await awardedMessage.ModifyAsync(embed);
    }

    private async Task DeleteAwardedMessage(DiscordChannel awardChannel, ulong awardedMessageId)
    {
        var awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

        if (awardedMessage == null)
        {
            return;
        }

        await awardedMessage.DeleteAsync();
    }

    private DiscordEmbed BuildAwardedMessageEmbed(DiscordMessage message, DiscordMember author, string channel)
    {
        var authorDisplayName = author.DisplayName;

        var messageContentSb = new StringBuilder();

        messageContentSb.AppendLine($"[**Jump to message!**]({message.JumpLink})");

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            messageContentSb.AppendLine();
            messageContentSb.AppendLine(message.Content);
        }

        var embedBuilder = new DiscordEmbedBuilder()
            .WithAuthor(authorDisplayName, null, author.GuildAvatarUrl ?? author.AvatarUrl)
            .WithFooter($"#{channel}")
            .WithTimestamp(message.Id)
            .WithColor(DiscordConstants.DefaultEmbedColor);

        if (message.Attachments.Count > 0)
        {
            var addedEmbedImage = false;

            foreach (var attachment in message.Attachments)
            {
                switch (attachment.MediaType)
                {
                    case string s when s.StartsWith("video"):
                        messageContentSb.AppendLine();
                        messageContentSb.AppendLine($"`Video attachment: {attachment.FileName}`");
                        break;
                    case string s when s.StartsWith("image"):
                        if (attachment.FileName is null)
                        {
                            messageContentSb.AppendLine();
                            messageContentSb.AppendLine("`Couldn't load image.`");
                            break;
                        }

                        if (attachment.FileName.StartsWith(SpoilerImageAttachmentPrefix))
                        {
                            messageContentSb.AppendLine();
                            messageContentSb.AppendLine("`Spoiler image hidden. Use the jump link to view it.`");
                        }
                        else if (!addedEmbedImage && attachment.Url is not null)
                        {
                            embedBuilder = embedBuilder.WithImageUrl(attachment.Url);
                            addedEmbedImage = true;
                        }

                        break;
                    default:
                        messageContentSb.AppendLine();
                        messageContentSb.AppendLine($"`File attachment: {attachment.FileName}`");
                        break;
                }
            }
        }
        else
        {
            // Try to fetch an image from message embed
            var messageEmbed = message.Embeds.FirstOrDefault();

            if (messageEmbed != null)
            {
                if (messageEmbed.Thumbnail?.ProxyUrl.HasValue ?? false)
                {
                    embedBuilder = embedBuilder.WithImageUrl(messageEmbed.Thumbnail.ProxyUrl.Value.ToUri()!);
                }

                if (!string.IsNullOrWhiteSpace(messageEmbed.Title) && messageEmbed.Url != null)
                {
                    var embededLinkText = $"[{messageEmbed.Title}]({messageEmbed.Url})";

                    messageContentSb.AppendLine();
                    messageContentSb.AppendLine(embededLinkText);
                }
            }
        }

        var messageContent = messageContentSb.ToString().Replace(" ", $"{InvisibleChar}");

        embedBuilder = embedBuilder.WithDescription(messageContent);

        var embed = embedBuilder.Build();

        return embed;
    }

    private async Task<uint> GetAwardReactionCount(DiscordMessage message, DiscordMember messageAuthor)
    {
        uint awardReactionCount = 0;

        try
        {
            var cookieEmoji = DiscordEmoji.FromUnicode(EmojiMap.Cookie);

            var cookieReactionUsers = await message.GetReactionsAsync(cookieEmoji);

            if (cookieReactionUsers != null)
            {
                var skipAuthor = false;

#if DEBUG
                skipAuthor = true;
#endif
                awardReactionCount = (uint)cookieReactionUsers
                    .Select(u => u.Id)
                    .Distinct()
                    .Count(x => skipAuthor || x != messageAuthor.Id);
            }
        }
        catch (Exception ex)
        {
            _discordErrorLogger.LogError(ex, nameof(GetAwardReactionCount));
        }

        return awardReactionCount;
    }
}
