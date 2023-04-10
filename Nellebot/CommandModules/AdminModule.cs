using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.Common.Extensions;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[RequireOwnerOrAdmin]
[Group("admin")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class AdminModule : BaseCommandModule
{
    private readonly ILogger<AdminModule> _logger;
    private readonly CommandQueueChannel _commandQueue;
    private readonly RequestQueueChannel _commandParallelQueue;
    private readonly BotOptions _options;

    public AdminModule(
        ILogger<AdminModule> logger,
        IOptions<BotOptions> options,
        CommandQueueChannel commandQueue,
        RequestQueueChannel commandParallelQueue)
    {
        _logger = logger;
        _commandQueue = commandQueue;
        _commandParallelQueue = commandParallelQueue;
        _options = options.Value;
    }

    [Command("nickname")]
    public Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
    {
        name = name.RemoveQuotes();

        return ctx.Guild.CurrentMember.ModifyAsync((props) =>
        {
            props.Nickname = name;
        });
    }

    [Command("list-award-channels")]
    public async Task ListCookieChannels(CommandContext ctx)
    {
        ulong[] groupIds = _options.AwardVoteGroupIds;

        if (groupIds == null)
        {
            return;
        }

        var sb = new StringBuilder();

        IReadOnlyList<DiscordChannel> guildChannels = await ctx.Guild.GetChannelsAsync();

        var channelGroups = new List<Tuple<string, List<DiscordChannel>>>();

        IEnumerable<DiscordChannel> categoryChannels = guildChannels
            .Where(c => c.Type == ChannelType.Category
                        && groupIds.Contains(c.Id));

        foreach (DiscordChannel? category in categoryChannels)
        {
            sb.AppendLine($"**{category.Name}**");

            IEnumerable<DiscordChannel> textChannelsForCategory = guildChannels.Where(c => c.Type == ChannelType.Text && c.ParentId == category.Id);

            foreach (DiscordChannel? channel in textChannelsForCategory)
            {
                sb.AppendLine($"#{channel.Name}");
            }

            sb.AppendLine();
        }

        await ctx.RespondAsync(sb.ToString());
    }

    [Command("add-missing-members")]
    public Task AddMissingMemberRoles(CommandContext ctx)
    {
        return _commandQueue.Writer.WriteAsync(new AddMissingMemberRolesCommand(ctx)).AsTask();
    }

    [Command("set-greeting-message")]
    public Task SetGreetingMessage(CommandContext ctx, [RemainingText] string message)
    {
        return _commandQueue.Writer.WriteAsync(new SetGreetingMessageCommand(ctx, message)).AsTask();
    }

    [Command("populate-messages")]
    public Task PopulateMessages(CommandContext ctx)
    {
        return _commandParallelQueue.Writer.WriteAsync(new PopulateMessagesCommand(ctx)).AsTask();
    }

    [Command("populate-user-log")]
    public Task PopulateUserLog(CommandContext ctx)
    {
        return _commandParallelQueue.Writer.WriteAsync(new PopulateUserLogCommand(ctx)).AsTask();
    }

    [Command("delete-spam-after")]
    public async Task DeleteSpam(CommandContext ctx, ulong channelId, ulong messageId)
    {
        DiscordChannel channel = ctx.Guild.GetChannel(channelId);

        IReadOnlyList<DiscordMessage> messagesToDelete = await channel.GetMessagesAfterAsync(messageId, 1000);

        await channel.DeleteMessagesAsync(messagesToDelete);

        await ctx.RespondAsync($"Deleted {messagesToDelete.Count} messages");
    }
}
