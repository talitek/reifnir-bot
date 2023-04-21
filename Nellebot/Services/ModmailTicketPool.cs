using System.Collections.Concurrent;
using System.Linq;
using Nellebot.Common.Models.Modmail;

namespace Nellebot.Services;

public class ModmailTicketPool
{
    private readonly ConcurrentBag<ModmailTicket> _ticketPool;

    public ModmailTicketPool()
    {
        _ticketPool = new ConcurrentBag<ModmailTicket>();

#if DEBUG
        //_ticketPool.Add(new ModmailTicket()
        //{
        //    ForumPostChannelId = 1099083673149657148,
        //    ForumPostMessageId = 1099083673149657148,
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
        return _ticketPool.SingleOrDefault(x => x.RequesterId == requesterId);
    }

    public ModmailTicket? GetTicketByChannelId(ulong channelId)
    {
        return _ticketPool.SingleOrDefault(x => x.ForumPostChannelId == channelId);
    }

    public bool TryAdd(ModmailTicket ticket)
    {
        var alreadyExists = _ticketPool.Any(x => x.RequesterId == ticket.RequesterId || x.ForumPostChannelId == ticket.ForumPostChannelId);

        if (alreadyExists) return false;

        _ticketPool.Add(ticket);

        return true;
    }
}
