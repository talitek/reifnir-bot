using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicket
{
    public Guid Id { get; set; }

    required public ulong RequesterId { get; set; }

    required public string RequesterDisplayName { get; set; }

    required public bool IsAnonymous { get; set; }

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public ModmailTicketPost? TicketPost { get; set; }

    public bool IsClosed { get; set; }

    public bool IsStub => TicketPost == null;
}

public record ModmailTicketPost(ulong ChannelThreadId, ulong MessageId);
