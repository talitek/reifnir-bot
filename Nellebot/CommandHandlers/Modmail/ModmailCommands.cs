using Nellebot.Common.Models.Modmail;

namespace Nellebot.CommandHandlers.Modmail;

public record RequestModmailTicketCommand : BaseCommand
{
    public RequestModmailTicketCommand(BaseContext ctx)
        : base(ctx) { }

    public RequestModmailTicketCommand(BaseContext ctx, string requestMessage)
        : base(ctx)
    {
        RequestMessage = requestMessage;
    }

    public string? RequestMessage { get; init; }
}

public record RelayModeratorMessageCommand(MessageContext Ctx, ModmailTicket Ticket) : MessageCommand(Ctx);
public record RelayRequesterMessageCommand(MessageContext Ctx, ModmailTicket Ticket) : MessageCommand(Ctx);
