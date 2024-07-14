using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Attributes;
using Nellebot.CommandHandlers;
using Nellebot.CommandHandlers.Modmail;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Extensions;
using Nellebot.Common.Models.Modmail;
using Nellebot.Data.Repositories;
using Nellebot.Utils;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheckV2]
[RequirePermissions(DiscordPermissions.None, UserPermissions = DiscordPermissions.Administrator)]
[Command("admin")]
public class AdminModule
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
    public async Task ChangeNickname(CommandContext ctx, [RemainingText] string name)
    {
        ctx.Guild.ThrowIfNull();

        name = name.RemoveQuotes();

        await ctx.Guild.CurrentMember.ModifyAsync(x => x.Nickname = name);

        // TODO implement a more general way to respond to commands
        if (ctx is SlashCommandContext slashCtx)
        {
            await slashCtx.RespondAsync("Nickname changed", true);
        }
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
        ctx.Guild.ThrowIfNull();

        DiscordChannel channel = await ctx.Guild.GetChannelAsync(channelId);

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
