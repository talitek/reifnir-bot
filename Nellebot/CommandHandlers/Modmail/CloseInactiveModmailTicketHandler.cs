using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers.Modmail;

public class CloseInactiveModmailTicketHandler : IRequestHandler<CloseInactiveModmailTicketCommand>
{
    private readonly DiscordResolver _resolver;

    public CloseInactiveModmailTicketHandler(DiscordResolver resolver)
    {
        _resolver = resolver;
    }

    public Task Handle(CloseInactiveModmailTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = request.Ticket;

        var ticketPost = ticket.TicketPost
            ?? throw new Exception("The ticket does not have a post channelId");

        var threadChannel = _resolver.ResolveThread(ticketPost.ChannelThreadId)
            ?? throw new Exception("Could not resolve thread channel");

        var ticketClosureMessage = "This ticket has been closed due to inactivity.";

        return threadChannel.SendMessageAsync(ticketClosureMessage);
    }
}
