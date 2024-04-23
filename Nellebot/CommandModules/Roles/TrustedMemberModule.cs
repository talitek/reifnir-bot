using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.Workers;

namespace Nellebot.CommandModules.Roles;

[BaseCommandCheck]
[RequireTrustedMember]
[ModuleLifespan(ModuleLifespan.Transient)]
public class TrustedMemberModule : BaseCommandModule
{
    private readonly CommandParallelQueueChannel _commandQueue;
    private readonly BotOptions _options;

    public TrustedMemberModule(CommandParallelQueueChannel commandQueue, IOptions<BotOptions> options)
    {
        _commandQueue = commandQueue;
        _options = options.Value;
    }

    [Command("vkick")]
    public async Task ValhallKick(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
    {
        await _commandQueue.Writer.WriteAsync(new ValhallKickUserCommand(ctx, member, reason));
    }

    [Command("list-award-channels")]
    public async Task ListCookieChannels(CommandContext ctx)
    {
        ulong[]? groupIds = _options.AwardVoteGroupIds;

        if (groupIds == null) return;

        var sb = new StringBuilder();

        IReadOnlyList<DiscordChannel> guildChannels = await ctx.Guild.GetChannelsAsync();

        var channelGroups = new List<Tuple<string, List<DiscordChannel>>>();

        IEnumerable<DiscordChannel> categoryChannels = guildChannels
            .Where(
                c => c.Type == DiscordChannelType.Category
                     && groupIds.Contains(c.Id));

        foreach (DiscordChannel category in categoryChannels)
        {
            sb.AppendLine($"**{category.Name}**");

            IEnumerable<DiscordChannel> textChannelsForCategory =
                guildChannels.Where(c => c.Type == DiscordChannelType.Text && c.ParentId == category.Id);

            foreach (DiscordChannel channel in textChannelsForCategory) sb.AppendLine($"#{channel.Name}");

            sb.AppendLine();
        }

        await ctx.RespondAsync(sb.ToString());
    }
}
