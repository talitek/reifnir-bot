using System.Collections.Concurrent;
using System.Collections.Generic;
using Nellebot.Common.Models.Modmail;

namespace Nellebot.Services;

public class ModmailTicketPool
{
    private readonly ConcurrentDictionary<string, ModmailTicket> _ticketPool;

    public ModmailTicketPool()
    {
        _ticketPool = new ConcurrentDictionary<string, ModmailTicket>();
    }

    public ModmailTicket? GetTicketForUser(ulong requesterId)
    {
        return _ticketPool.GetValueOrDefault(requesterId.ToString());
    }

    public bool Add(ModmailTicket ticket)
    {
        return _ticketPool.TryAdd(ticket.RequesterId.ToString(), ticket);
    }
}
