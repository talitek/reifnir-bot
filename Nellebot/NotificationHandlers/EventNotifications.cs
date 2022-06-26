using DSharpPlus.EventArgs;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public abstract class EventNotification : INotification
    {
    }

    public class GuildMemberUpdatedNotification : EventNotification
    {
        public GuildMemberUpdateEventArgs EventArgs { get; set; }

        public GuildMemberUpdatedNotification(GuildMemberUpdateEventArgs eventArgs)
        {
            EventArgs = eventArgs;
        }
    }
}
