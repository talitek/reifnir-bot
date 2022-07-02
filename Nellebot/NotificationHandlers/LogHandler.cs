using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class LogHandler : INotificationHandler<GuildBanAddedNotification>,
                                INotificationHandler<GuildMemberRemovedNotification>,
                                INotificationHandler<MessageDeletedNotification>
    {
        private readonly DiscordLogger _discordLogger;
        private readonly DiscordResolver _discordResolver;

        public LogHandler(DiscordLogger discordLogger, DiscordResolver discordResolver)
        {
            _discordLogger = discordLogger;
            _discordResolver = discordResolver;
        }

        public async Task Handle(GuildBanAddedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberName = args.Member.GetNicknameOrDisplayName();

            var auditBanEntry = (await GetAuditLogEntries<DiscordAuditLogBanEntry>(args.Guild, AuditLogActionType.Kick))
                    .FirstOrDefault(x => x.Target.Id == args.Member.Id);

            if (auditBanEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditBanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"**{memberName}** was banned by **{memberResponsible.Username}**. Reason: {auditBanEntry.Reason}.");
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberName = args.Member.GetNicknameOrDisplayName();

            // It's possible that the audit log entry might not be available right away.
            // If that is the case, consider wrapping this call into some sort of exeponential backoff retry.
            var auditKickEntry = (await GetAuditLogEntries<DiscordAuditLogKickEntry>(args.Guild, AuditLogActionType.Kick))
                                .FirstOrDefault(x => x.Target.Id == args.Member.Id);

            if (auditKickEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditKickEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"**{memberName}** was kicked by **{responsibleName}**. Reason: {auditKickEntry.Reason}.");
        }

        public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var message = args.Message;
            var channel = args.Channel;

            if (channel.IsPrivate) return;

            var authorAsMember = await _discordResolver.ResolveGuildMember(args.Guild, message.Author.Id);

            if (authorAsMember == null) return;

            var authorName = authorAsMember.GetNicknameOrDisplayName();

            var auditMessageDeleteEntry = (await GetAuditLogEntries<DiscordAuditLogMessageEntry>(args.Guild, AuditLogActionType.MessageDelete))
                    .FirstOrDefault(x => x.Target.Id == authorAsMember.Id);

            if (auditMessageDeleteEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"Message written by **{authorName}** in **{channel.Name}** was removed by **{responsibleName}**.");
        }

        private async Task<IEnumerable<T>> GetAuditLogEntries<T>(DiscordGuild guild, AuditLogActionType logType)
        {
            return (await guild.GetAuditLogsAsync(limit: 10, by_member: null, action_type: logType))
                .Where(x => x.CreationTimestamp > DateTimeOffset.UtcNow.AddMinutes(-5))
                .Cast<T>();
        }
    }
}
