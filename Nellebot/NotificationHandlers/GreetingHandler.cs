using MediatR;
using Nellebot.Services;
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
        private readonly BotSettingsService _botSettingsService;

        public GreetingHandler(DiscordLogger discordLogger, BotSettingsService botSettingsService)
        {
            _discordLogger = discordLogger;
            _botSettingsService = botSettingsService;
        }

        public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
        {
            var memberMention = notification.EventArgs.Member.Mention;

            var greetingMessage = await _botSettingsService.GetGreetingsMessage(memberMention);

            if (greetingMessage == null) throw new Exception("Could not load greeting message");

            await _discordLogger.LogGreetingMessage(greetingMessage);
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var memberName = notification.EventArgs.Member.GetNicknameOrDisplayName();

            await _discordLogger.LogGreetingMessage($"**{memberName}** has left the server. Goodbye!");
        }
    }
}
