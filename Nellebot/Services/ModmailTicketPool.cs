using System;
using System.Collections.Concurrent;
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
        //TryAdd(new ModmailTicket()
        //{
        //    TicketPost = new ModmailTicketPost(1099083673149657148, 1099083673149657148),
        //    RequesterId = 78474916734701568,
        //    //RequesterDisplayName = "Test",
        //    //IsAnonymous = false,
        //    RequesterDisplayName = "Hugh Mongous",
        //    IsAnonymous = true,
        //});
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

    public ModmailTicket AddOrUpdate(ModmailTicket ticket)
    {
        return _ticketPool.AddOrUpdate(ticket.Id, ticket, (key, oldValue) => ticket);
    }

    public void Clear()
    {
        _ticketPool.Clear();
    }
}
