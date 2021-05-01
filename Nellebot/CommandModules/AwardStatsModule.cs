using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.Data.Repositories;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    [Group("cookie-stats")]
    public class AwardStatsModule : BaseCommandModule
    {
        private readonly DiscordResolver _discordResolver;
        private readonly BotOptions _options;
        private readonly AwardMessageRepository _awardMessageRepo;
        private readonly SharedCache _cache;
        private const int _maxMessageLength = 50;

        public AwardStatsModule(
            DiscordResolver discordResolver,
            IOptions<BotOptions> options,
            AwardMessageRepository awardMessageRepo,
            SharedCache cache
            )
        {
            _discordResolver = discordResolver;
            _options = options.Value;
            _awardMessageRepo = awardMessageRepo;
            _cache = cache;
        }

        [Command("me")]
        public async Task GetUserAwardStats(CommandContext ctx)
        {
            var member = ctx.Member;
            var userId = member.Id;
            var guild = ctx.Guild;

            var mention = ctx.Member.GetNicknameOrDisplayName();

            var userAwardStats = await _awardMessageRepo.GetAwardStatsForUser(userId);

            var sb = new StringBuilder();

            var embedBuilder = new DiscordEmbedBuilder()
                .WithAuthor(mention, null, member.AvatarUrl)
                .WithTitle("Cookie stats")
                .WithColor(9648895); // #933aff 

            if (userAwardStats.TotalAwardCount == 0)
            {
                embedBuilder = embedBuilder.WithDescription("No awarded messages");

                await ctx.RespondAsync(embedBuilder.Build());

                return;
            }

            sb.AppendLine($"Total cookies: {userAwardStats.TotalAwardCount}");
            sb.AppendLine($"Messages in cookie channel: {userAwardStats.AwardMessageCount}");

            sb.AppendLine();
            sb.AppendLine("Top messages");
            foreach (var awardedMessage in userAwardStats.TopAwardedMessages)
            {
                sb.Append($"{DiscordEmoji.FromUnicode(EmojiMap.Cookie).Name} **{awardedMessage.AwardCount}** ");

                var channelId = awardedMessage.OriginalChannelId;

                var messageChannel = await _cache.LoadFromCacheAsync(
                    string.Format(SharedCacheKeys.DiscordChannel, channelId),
                    async () => await _discordResolver.ResolveChannel(guild, channelId),
                    TimeSpan.FromSeconds(10));

                if (messageChannel == null) continue;

                var messageResolveResult = await _discordResolver.TryResolveMessage(messageChannel, awardedMessage.OriginalMessageId);

                if (messageResolveResult.Resolved)
                {
                    var message = messageResolveResult.Result;

                    var shortenedMessage = message.Content.Length > _maxMessageLength
                        ? $"{message.Content.Substring(0, _maxMessageLength)}..."
                        : message.Content;

                    if (string.IsNullOrWhiteSpace(shortenedMessage))
                        shortenedMessage = "*no message text*";

                    sb.AppendLine($"[{shortenedMessage}]({message.JumpLink})");
                }
                else
                {
                    sb.AppendLine("Message not found");
                }
            }

            embedBuilder = embedBuilder.WithDescription(sb.ToString());

            await ctx.RespondAsync(embedBuilder.Build());
        }
    }
}
