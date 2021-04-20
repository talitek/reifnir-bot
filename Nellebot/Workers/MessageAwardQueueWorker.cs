using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;

namespace Nellebot.Workers
{
    public class MessageAwardQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<MessageAwardQueueWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly MessageAwardQueue _awardQueue;
        private readonly BotOptions _options;

        public MessageAwardQueueWorker(
                ILogger<MessageAwardQueueWorker> logger,
                IServiceProvider serviceProvider,
                DiscordErrorLogger discordErrorLogger,
                IOptions<BotOptions> options,
                MessageAwardQueue awardQueue
            )
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _discordErrorLogger = discordErrorLogger;
            _awardQueue = awardQueue;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_awardQueue.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _awardQueue.TryDequeue(out var awardMessage);

                    if (awardMessage != null)
                    {
                        _logger.LogDebug($"Dequeued message. {_awardQueue.Count} left in queue");

                        await HandleAwardMessage(awardMessage);

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, "MessageAwardQueueWorker");

                        var escapedError = DiscordErrorLogger.ReplaceTicks(ex.ToString());
                        await _discordErrorLogger.LogDiscordError($"`{escapedError}`");
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }

        private async Task HandleAwardMessage(MessageAwardQueueItem awardItem)
        {
            // TODO optimize by resolving full message only if it will be posted
            var message = await ResolveFullMessage(awardItem.DiscordMessage);

            if (message == null)
            {
                _logger.LogDebug("Could not resolve message");
                return;
            }

            var channel = message.Channel;
            var guild = channel.Guild;

            var allowedGroupIds = _options.AwardVoteGroupIds;

            if (allowedGroupIds == null || allowedGroupIds.Length == 0)
            {
                _logger.LogDebug($"{nameof(_options.AwardVoteGroupIds)} is empty");
                return;
            }

            var isAllowedChannel = allowedGroupIds.ToList().Contains(channel.ParentId!.Value);

            if (!isAllowedChannel)
            {
                _logger.LogDebug("Award reaction added in non-whitelisted channel group");
                return;
            }

            var cookieEmoji = DiscordEmoji.FromUnicode(EmojiMap.Cookie);

            var awardReactionCount = 0;

            var messageAuthor = await ResolveGuildMember(guild, message.Author.Id);

            if (messageAuthor == null)
            {
                _logger.LogDebug("Could not resolve message author");
                return;
            }

            try
            {
                var cookieReactionUsers = await message.GetReactionsAsync(cookieEmoji);

                if (cookieReactionUsers != null)
                {
                    awardReactionCount = cookieReactionUsers.Count(u => u.Id != messageAuthor.Id);
                }

            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());
            }

            var hasEnoughAwards = awardReactionCount >= _options.RequiredAwardCount;

            if (!hasEnoughAwards)
            {
                _logger.LogDebug($"Not enough awards. {awardReactionCount} / {_options.RequiredAwardCount}");
                return;
            }

            await PostAwardMessage(message, messageAuthor, awardReactionCount);
        }

        private async Task PostAwardMessage(DiscordMessage message, DiscordMember author, int awardCount)
        {
            var guild = message.Channel.Guild;
            var messageChannel = message.Channel;

            var awardChannelId = _options.AwardChannelId;
            var authorDisplayName = author.GetNicknameOrDisplayName();

            var awardChannel = await ResolveAwardChannel(guild, awardChannelId);

            if (awardChannel == null)
            {
                _logger.LogDebug("Could not resolve award channel");
                return;
            }

            // TODO check if message already exists before posting

            var awardText = $"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardCount}**";
            var messageRef = $"[ID:{message.Id}]";

            var resultText = $"{awardText} {messageRef}";

            var messageLink = $"[**Jump to message!**]({message.JumpLink})";

            var description = $"{messageLink}\r\n\r\n{message.Content}";

            var embed = new DiscordEmbedBuilder()           
                .WithAuthor(authorDisplayName, null, author.AvatarUrl)
                .WithDescription(description)
                .WithFooter($"#{messageChannel.Name}")
                .WithTimestamp(message.Id)                
                .WithColor(9648895) // #933aff 
                .Build();

            await awardChannel.SendMessageAsync(resultText, embed);
        }

        private async Task<DiscordChannel?> ResolveAwardChannel(DiscordGuild guild, ulong channelId)
        {
            guild.Channels.TryGetValue(channelId, out var discordChannel);

            if (discordChannel == null)
            {
                try
                {
                    discordChannel = guild.GetChannel(channelId);
                }
                catch (Exception ex)
                {
                    await _discordErrorLogger.LogDiscordError(ex.ToString());

                    return null;
                }
            }

            return discordChannel;
        }

        private async Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
        {
            var memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

            if (memberExists)
                return member;

            try
            {
                return await guild.GetMemberAsync(userId);
            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());

                return null;
            }
        }

        private async Task<DiscordMessage?> ResolveFullMessage(DiscordMessage partialMessage)
        {
            try
            {
                var fullMessage = await partialMessage.Channel.GetMessageAsync(partialMessage.Id);

                return fullMessage;
            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());

                return null;
            }
        }
    }
}
