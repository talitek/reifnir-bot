using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nellebot.Data.Repositories;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class CloseInactiveModmailTicketHandler : IRequestHandler<CloseInactiveModmailTicketCommand>
{
    private readonly DiscordResolver _resolver;
    private readonly ModmailTicketRepository _modmailTicketRepo;

    public CloseInactiveModmailTicketHandler(DiscordResolver resolver, ModmailTicketRepository modmailTicketRepo)
    {
        _resolver = resolver;
        _modmailTicketRepo = modmailTicketRepo;
    }

    public async Task Handle(CloseInactiveModmailTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = request.Ticket;

        var ticketPost = ticket.TicketPost
            ?? throw new Exception("The ticket does not have a post channelId");

        var threadChannel = _resolver.ResolveThread(ticketPost.ChannelThreadId)
            ?? throw new Exception("Could not resolve thread channel");

        await _modmailTicketRepo.CloseTicket(ticket, cancellationToken);

        var ticketClosureMessage = "This ticket has been closed due to inactivity.";

        await threadChannel.SendMessageAsync(ticketClosureMessage);
    }
}
