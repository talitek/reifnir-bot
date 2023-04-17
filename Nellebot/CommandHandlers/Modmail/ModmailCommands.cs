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

public record RelayModeratorMessageCommand(BaseContext Ctx, ModmailTicket Ticket, string Message) : BaseCommand(Ctx);
public record RelayRequesterMessageCommand(BaseContext Ctx, ModmailTicket Ticket, string Message) : BaseCommand(Ctx);
