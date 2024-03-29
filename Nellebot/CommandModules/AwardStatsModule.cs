using System;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nellebot.Attributes;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Utils;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[Group("cookie-stats")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class AwardStatsModule : BaseCommandModule
{
    private const int MaxMessageLength = 50;
    private readonly AwardMessageRepository _awardMessageRepo;
    private readonly SharedCache _cache;

    private readonly DiscordResolver _discordResolver;

    public AwardStatsModule(
        DiscordResolver discordResolver,
        AwardMessageRepository awardMessageRepo,
        SharedCache cache)
    {
        _discordResolver = discordResolver;
        _awardMessageRepo = awardMessageRepo;
        _cache = cache;
    }

    [Command("me")]
    public async Task GetUserAwardStatsSelf(CommandContext ctx)
    {
        var member = ctx.Member;

        if (member is null)
        {
            await ctx.RespondAsync("Could not fetch user");
            return;
        }

        await GetUserAwardStats(ctx, member);
    }

    [GroupCommand]
    public async Task GetUserAwardStatsOtherUser(CommandContext ctx, DiscordUser user)
    {
        var member = await _discordResolver.ResolveGuildMember(ctx.Guild, user.Id);

        if (member is null)
        {
            await ctx.RespondAsync("Could not fetch user");
            return;
        }

        await GetUserAwardStats(ctx, member);
    }

    private async Task GetUserAwardStats(CommandContext ctx, DiscordMember member)
    {
        var userId = member.Id;
        var guild = ctx.Guild;

        var mention = member.DisplayName;

        var userAwardStats = await _awardMessageRepo.GetAwardStatsForUser(userId);

        var sb = new StringBuilder();

        // Library does not support nullable reference types.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var embedBuilder = new DiscordEmbedBuilder()
            .WithAuthor(mention, null, member.GuildAvatarUrl ?? member.AvatarUrl)
            .WithTitle("Cookie stats")
            .WithColor(DiscordConstants.DefaultEmbedColor);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

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
                                                                 string.Format(
                                                                               SharedCacheKeys.DiscordChannel,
                                                                               channelId),
                                                                 async () =>
                                                                     await _discordResolver
                                                                         .ResolveChannelAsync(channelId),
                                                                 TimeSpan.FromSeconds(10));

            if (messageChannel is null) continue;

            var messageResolveResult =
                await _discordResolver.TryResolveMessage(messageChannel, awardedMessage.OriginalMessageId);

            if (messageResolveResult.Resolved)
            {
                var message = messageResolveResult.Value;

                var shortenedMessage = message.Content.Length > MaxMessageLength
                    ? $"{message.Content.Substring(0, MaxMessageLength)}..."
                    : message.Content;

                if (string.IsNullOrWhiteSpace(shortenedMessage))
                {
                    shortenedMessage = "*no message text*";
                }

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
