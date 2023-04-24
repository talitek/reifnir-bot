using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.Common.Extensions;
using Nellebot.Services;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[RequireOwnerOrAdmin]
[Group("admin")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class AdminModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandQueue;
    private readonly RequestQueueChannel _commandParallelQueue;
    private readonly ModmailTicketPool _ticketPool;
    private readonly IMediator _mediator;

    public AdminModule(
        CommandQueueChannel commandQueue,
        RequestQueueChannel commandParallelQueue,
        ModmailTicketPool ticketPool,
        IMediator mediator)
    {
        _commandQueue = commandQueue;
        _commandParallelQueue = commandParallelQueue;
        _ticketPool = ticketPool;
        _mediator = mediator;
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

    [Command("delete-spam-after")]
    public async Task DeleteSpam(CommandContext ctx, ulong channelId, ulong messageId)
    {
        DiscordChannel channel = ctx.Guild.GetChannel(channelId);

        IReadOnlyList<DiscordMessage> messagesToDelete = await channel.GetMessagesAfterAsync(messageId, 1000);

        await channel.DeleteMessagesAsync(messagesToDelete);

        await ctx.RespondAsync($"Deleted {messagesToDelete.Count} messages");
    }

    [Command("modmail-purge")]
    public Task PurgeModmail(CommandContext ctx)
    {
        var purged = _ticketPool.Clear();

        return ctx.Channel.SendMessageAsync($"Purged {purged} tickets");
    }

    [Command("modmail-close-all")]
    public async Task CloseAll(CommandContext ctx)
    {
        var expiredTickets = _ticketPool.RemoveInactiveTickets(TimeSpan.FromSeconds(1)).ToList();

        foreach (var ticket in expiredTickets)
        {
            await _mediator.Send(new CloseInactiveModmailTicketCommand(ticket));
        }

        await ctx.Channel.SendMessageAsync($"Closed {expiredTickets.Count} tickets");
    }
}
