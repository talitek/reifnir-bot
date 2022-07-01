using MediatR;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class GreetingHandler :
        INotificationHandler<GuildMemberAddedNotification>,
        INotificationHandler<GuildMemberRemovedNotification>
    {
        private readonly DiscordLogger _discordLogger;

        public GreetingHandler(DiscordLogger discordLogger)
        {
            _discordLogger = discordLogger;
        }

        public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
        {
            var memberName = notification.EventArgs.Member.GetNicknameOrDisplayName();

            await _discordLogger.LogGreetingMessage($"Hello, {memberName}");
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var memberName = notification.EventArgs.Member.GetNicknameOrDisplayName();

            await _discordLogger.LogGreetingMessage($"Goodbye, {memberName}");
        }
    }
}
