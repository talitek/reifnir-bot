using System;

namespace Nellebot.Common.Models.Modmail;

public record ModmailTicket
{
    public Guid Id { get; set; }

    public ulong RequesterId { get; init; }

    public string RequesterDisplayName { get; set; } = null!;

    public bool IsAnonymous { get; set; }

    public ulong? ForumPostChannelId { get; set; }

    public ulong? ForumPostMessageId { get; set; }
}
