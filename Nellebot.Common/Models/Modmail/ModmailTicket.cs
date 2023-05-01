using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicketEntity
{
    public Guid Id { get; init; }

    public ulong? RequesterIdPlain { get; init; }

    public ulong? RequesterIdEncrypted { get; init; }

    public string RequesterDisplayName { get; init; } = null!;

    public DateTime LastActivity { get; init; } = DateTime.UtcNow;

    public ModmailTicketPost? TicketPost { get; init; }
}

public record ModmailTicket
{
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
