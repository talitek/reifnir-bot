using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicket
{
    public Guid Id { get; set; }

    public ulong RequesterId { get; init; }

    public string RequesterDisplayName { get; set; } = null!;

    public bool IsAnonymous { get; set; }

    public ulong? ThreadId { get; set; }
}

public enum MessageAuthorType
{
    Requester = 1,
    Moderator = 2,
}

public record ModmailTicketMessage
{
    public ulong AuthorId { get; init; }

    public MessageAuthorType AuthorType { get; init; }

    public ulong OriginalMessageId { get; init; }

    public DateTime DateTime { get; init; }
}
