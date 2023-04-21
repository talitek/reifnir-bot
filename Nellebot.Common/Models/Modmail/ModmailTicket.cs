using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicket
{
    public Guid Id { get; set; }

    public ulong RequesterId { get; init; }

    public string RequesterDisplayName { get; set; } = null!;

    public bool IsAnonymous { get; set; }

    public ModmailTicketPost? TicketPost { get; set; }

    public bool IsStub => TicketPost == null;
}

public record ModmailTicketPost(ulong ChannelThreadId, ulong MessageId);
