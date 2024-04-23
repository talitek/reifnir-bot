using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Extensions;
using Nellebot.Common.Models.Modmail;
using Nellebot.Data.Repositories;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[RequireOwnerOrAdmin]
[Group("admin")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class AdminModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandQueue;
    private readonly IMediator _mediator;
    private readonly ModmailTicketRepository _modmailTicketRepo;

    public AdminModule(
        CommandQueueChannel commandQueue,
        ModmailTicketRepository modmailTicketRepo,
        IMediator mediator)
    {
        _commandQueue = commandQueue;
        _modmailTicketRepo = modmailTicketRepo;
        _mediator = mediator;
    }

    [Command("nickname")]
    public Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
    {
        name = name.RemoveQuotes();

        return ctx.Guild.CurrentMember.ModifyAsync(props => { props.Nickname = name; });
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
        return _commandQueue.Writer.WriteAsync(new PopulateMessagesCommand(ctx)).AsTask();
    }

    [Command("delete-spam-after")]
    public async Task DeleteSpam(CommandContext ctx, ulong channelId, ulong messageId)
    {
        DiscordChannel channel = ctx.Guild.GetChannel(channelId);

        var messagesToDelete = new List<DiscordMessage>();
        await foreach (DiscordMessage m in channel.GetMessagesAfterAsync(messageId, 1000))
        {
            messagesToDelete.Add(m);
        }

        await channel.DeleteMessagesAsync(messagesToDelete);

        await ctx.RespondAsync($"Deleted {messagesToDelete.Count} messages");
    }

    [Command("modmail-close-all")]
    public async Task CloseAll(CommandContext ctx)
    {
        List<ModmailTicket> expiredTickets = await _modmailTicketRepo.GetOpenExpiredTickets(TimeSpan.FromSeconds(1));

        foreach (ModmailTicket ticket in expiredTickets)
        {
            await _mediator.Send(new CloseInactiveModmailTicketCommand(ticket));
        }

        await ctx.Channel.SendMessageAsync($"Closed {expiredTickets.Count} tickets");
    }

    [Command("rebuild-ordbok")]
    public Task RebuildOrbok(CommandContext ctx)
    {
        return _commandQueue.Writer.WriteAsync(new RebuildArticleStoreCommand(ctx)).AsTask();
    }
}
