using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Nellebot.Common.Models.Modmail;

namespace Nellebot.Services;

public class ModmailTicketPool
{
    private readonly ConcurrentDictionary<Guid, ModmailTicket> _ticketPool;

    public ModmailTicketPool()
    {
        _ticketPool = new ConcurrentDictionary<Guid, ModmailTicket>();

#if DEBUG
        // TryAdd(new ModmailTicket()
        // {
        //    TicketPost = new ModmailTicketPost(1099083673149657148, 1099083673149657148),
        //    RequesterId = 78474916734701568,
        //    //RequesterDisplayName = "Test",
        //    //IsAnonymous = false,
        //    RequesterDisplayName = "Hugh Mongous",
        //    IsAnonymous = true,
        // });
#endif
    }

    public ModmailTicket? GetTicketByUserId(ulong requesterId)
    {
        return _ticketPool.SingleOrDefault(x => x.Value.RequesterId == requesterId).Value;
    }

    public ModmailTicket? GetTicketByChannelId(ulong channelId)
    {
        return _ticketPool.SingleOrDefault(x => x.Value.TicketPost?.ChannelThreadId == channelId).Value;
    }

    public bool TryAdd(ModmailTicket ticket)
    {
        return _ticketPool.TryAdd(ticket.Id, ticket);
    }

    public bool TryRemove(ModmailTicket ticket)
    {
        return _ticketPool.TryRemove(ticket.Id, out _);
    }

    public ModmailTicket AddOrUpdate(ModmailTicket ticket)
    {
        return _ticketPool.AddOrUpdate(ticket.Id, ticket, (key, oldValue) => ticket);
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
