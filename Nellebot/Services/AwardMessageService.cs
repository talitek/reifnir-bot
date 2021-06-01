using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Data.Repositories;
using Nellebot.Helpers;
using Nellebot.Utils;
using Nellebot.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class AwardMessageService
    {
        private readonly AwardMessageRepository _awardMessageRepo;
        private readonly ILogger<AwardMessageService> _logger;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly DiscordResolver _discordResolver;
        private readonly BotOptions _options;

        public AwardMessageService(
            AwardMessageRepository awardMessageRepo,
            ILogger<AwardMessageService> logger,
            IOptions<BotOptions> options,
            DiscordErrorLogger discordErrorLogger,
            DiscordResolver discordResolver
            )
        {
            _awardMessageRepo = awardMessageRepo;
            _logger = logger;
            _discordErrorLogger = discordErrorLogger;
            _discordResolver = discordResolver;
            _options = options.Value;
        }

        public async Task HandleAwardChange(MessageAwardQueueItem awardItem)
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

            if (messageAuthor == null)
            {
                _logger.LogDebug("Could not resolve message author");
                return;
            }

            var awardChannel = await _discordResolver.ResolveChannel(guild, _options.AwardChannelId);

            if (awardChannel == null)
            {
                _logger.LogDebug("Could not resolve awards channel");
                return;
            }

            uint awardReactionCount = await GetAwardReactionCount(message, messageAuthor);

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

                await _awardMessageRepo.CreateAwardMessage(message.Id, message.ChannelId, postedMessage.Id, awardChannel.Id, messageAuthor.Id, awardReactionCount);
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

        public async Task HandleAwardMessageUpdated(MessageAwardQueueItem awardItem)
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

            if (messageAuthor == null)
            {
                _logger.LogDebug("Could not resolve message author");
                return;
            }

            var awardChannel = await _discordResolver.ResolveChannel(guild, _options.AwardChannelId);

            if (awardChannel == null)
            {
                _logger.LogDebug("Could not resolve awards channel");
                return;
            }

            uint awardReactionCount = await GetAwardReactionCount(message, messageAuthor);

            var hasAwards = awardReactionCount > 0;

            if (!hasAwards)
            {
                _logger.LogDebug($"Updated message has no awards");
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


        public async Task HandleAwardMessageDeleted(MessageAwardQueueItem awardItem)
        {
            var messageId = awardItem.DiscordMessageId;

            var channel = awardItem.DiscordChannel!;
            var guild = channel.Guild;

            var awardChannel = await _discordResolver.ResolveChannel(guild, _options.AwardChannelId);

            if (awardChannel == null)
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

        private async Task<DiscordMessage> PostAwardedMessage(DiscordChannel awardChannel, DiscordMessage originalMessage, DiscordMember author, uint awardCount)
        {
            var messageChannel = originalMessage.Channel;

            var embed = BuildAwardedMessageEmbed(originalMessage, author, messageChannel.Name);

            var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";

            var messageText = $"{awardText}";

            return await awardChannel.SendMessageAsync(messageText, embed);
        }

        private async Task UpdateAwardedMessageText(DiscordChannel awardChannel, ulong awardedMessageId, uint awardCount)
        {
            var awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

            if (awardedMessage == null)
                return;

            var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";

            var messageText = $"{awardText}";

            await awardedMessage.ModifyAsync(messageText);
        }

        private async Task UpdateAwardedMessageEmbed(DiscordChannel awardChannel, ulong awardedMessageId, DiscordMessage originalMessage, DiscordMember author)
        {
            var messageChannel = originalMessage.Channel;

            var awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

            if (awardedMessage == null)
                return;

            var embed = BuildAwardedMessageEmbed(originalMessage, author, messageChannel.Name);

            await awardedMessage.ModifyAsync(embed);
        }

        private async Task DeleteAwardedMessage(DiscordChannel awardChannel, ulong awardedMessageId)
        {
            var awardedMessage = await _discordResolver.ResolveMessage(awardChannel, awardedMessageId);

            if (awardedMessage == null)
                return;

            await awardedMessage.DeleteAsync();
        }

        private DiscordEmbed BuildAwardedMessageEmbed(DiscordMessage message, DiscordMember author, string channel)
        {
            var authorDisplayName = author.GetNicknameOrDisplayName();

            var messageLink = $"[**Jump to message!**]({message.JumpLink})";

            var messageContent = $"{messageLink}\r\n\r\n{message.Content}";

            var embedBuilder = new DiscordEmbedBuilder()
                .WithAuthor(authorDisplayName, null, author.AvatarUrl)
                .WithFooter($"#{channel}")
                .WithTimestamp(message.Id)
                .WithColor(9648895); // #933aff 

            var attachment = message.Attachments.FirstOrDefault();

            if (attachment != null)
            {
                embedBuilder = embedBuilder.WithImageUrl(attachment.Url);
            }
            else
            {
                // Try to fetch an image from message embed
                var messageEmbed = message.Embeds.FirstOrDefault();

                if (messageEmbed != null)
                {
                    if (messageEmbed.Thumbnail != null)
                        embedBuilder = embedBuilder.WithImageUrl(messageEmbed.Thumbnail.ProxyUrl.ToUri());

                    if (!string.IsNullOrWhiteSpace(messageEmbed.Title) && messageEmbed.Url != null)
                    {
                        var embededLinkText = $"[{messageEmbed.Title}]({messageEmbed.Url})";

                        messageContent += $"\r\n\r\n{embededLinkText}";
                    }
                }
            }

            embedBuilder = embedBuilder.WithDescription(messageContent);

            var embed = embedBuilder.Build();

            return embed;
        }

        private async Task<uint> GetAwardReactionCount(DiscordMessage message, DiscordMember? messageAuthor)
        {
            uint awardReactionCount = 0;

            try
            {
                var cookieEmoji = DiscordEmoji.FromUnicode(EmojiMap.Cookie);

                var cookieReactionUsers = await message.GetReactionsAsync(cookieEmoji);

                var userCountTest = cookieReactionUsers.Count();
                var userCountTest2 = (uint)cookieReactionUsers.Count();
                var userCountTest3 = cookieReactionUsers.Count;

                _logger.LogDebug($"Cookie reaction count 1: {userCountTest}");
                _logger.LogDebug($"Cookie reaction count 2: {userCountTest2}");
                _logger.LogDebug($"Cookie reaction count 3: {userCountTest3}");

                if (cookieReactionUsers != null)
                {
                    var skipAuthor = false;

#if DEBUG
                    skipAuthor = true;
#endif
                    awardReactionCount = (uint)cookieReactionUsers.Count(u => skipAuthor || u.Id != messageAuthor.Id);
                }
            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());
            }

            return awardReactionCount;
        }



    }
}
