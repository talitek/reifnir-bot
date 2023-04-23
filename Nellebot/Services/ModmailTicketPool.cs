using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nellebot.Common.Models.Modmail;

namespace Nellebot.Services;

public class ModmailTicketPool
{
    private readonly ConcurrentDictionary<ulong, ModmailTicket> _ticketPool;

    public ModmailTicketPool()
    {
        _ticketPool = new ConcurrentDictionary<ulong, ModmailTicket>();
    }

    public ModmailTicket? Get(ulong requesterId)
    {
        _ticketPool.TryGetValue(requesterId, out var ticket);

        return ticket;
    }

    public ModmailTicket? GetTicketByChannelId(ulong channelId)
    {
        return _ticketPool.SingleOrDefault(x => x.Value.TicketPost?.ChannelThreadId == channelId).Value;
    }

    public bool TryAdd(ModmailTicket ticket)
    {
        return _ticketPool.TryAdd(ticket.RequesterId, ticket);
    }

    public bool TryRemove(ModmailTicket ticket)
    {
        return _ticketPool.TryRemove(ticket.RequesterId, out _);
    }

    public bool TryUpdate(ModmailTicket ticket)
    {
        var current = _ticketPool[ticket.RequesterId];

        return _ticketPool.TryUpdate(ticket.RequesterId, ticket, current);
    }

    public int Clear()
    {
        var currentCount = _ticketPool.Count;

        _ticketPool.Clear();

        return currentCount;
    }

    public IEnumerable<ModmailTicket> RemoveInactiveTickets(TimeSpan expiryThreshold)
    {
        var removed = new List<ModmailTicket>();

        _ticketPool
            .Where(x => (DateTime.UtcNow - x.Value.LastActivity) > expiryThreshold)
            .ToList()
            .ForEach(x =>
            {
                if (_ticketPool.TryRemove(x.Key, out var ticket))
                {
                    removed.Add(ticket);
                }
            });

        return removed;
    }
}
