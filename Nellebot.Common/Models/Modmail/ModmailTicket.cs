using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicket
{
    required public Guid Id { get; init; }

    required public ulong RequesterId { get; init; }

    required public string RequesterDisplayName { get; init; }

    required public bool IsAnonymous { get; init; }

    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;

    public ModmailTicketPost? TicketPost { get; init; }

    public bool IsStub => TicketPost == null;

    public ModmailTicket RefreshLastActivity()
    {
        return this with
        {
            LastActivity = DateTime.UtcNow,
        };
    }
}

public record ModmailTicketPost(ulong ChannelThreadId, ulong MessageId);
