using DSharpPlus.Entities;
using MediatR;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class BotLogHandler : INotificationHandler<GuildBanAddedNotification>,
                                INotificationHandler<GuildBanRemovedNotification>,
                                INotificationHandler<GuildMemberRemovedNotification>,
                                INotificationHandler<MessageDeletedNotification>,
                                INotificationHandler<MessageBulkDeletedNotification>
    {
        private readonly DiscordLogger _discordLogger;
        private readonly DiscordResolver _discordResolver;

        public BotLogHandler(DiscordLogger discordLogger, DiscordResolver discordResolver)
        {
            _discordLogger = discordLogger;
            _discordResolver = discordResolver;
        }

        public async Task Handle(GuildBanAddedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberName = args.Member.GetNicknameOrDisplayName();
            var memberIdentifier = args.Member.GetDetailedMemberIdentifier();

            var auditBanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>
                                    (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == args.Member.Id);

            if (auditBanEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditBanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"**{memberName}** was banned by **{responsibleName}**. Reason: {auditBanEntry.Reason}. [{memberIdentifier}]");
        }

        public async Task Handle(GuildBanRemovedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberName = args.Member.GetNicknameOrDisplayName();
            var memberIdentifier = args.Member.GetDetailedMemberIdentifier();

            var auditUnbanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>
                                    (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == args.Member.Id);

            if (auditUnbanEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditUnbanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"**{memberName}** was unbanned by **{responsibleName}**. [{memberIdentifier}]");
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            // It's possible that the audit log entry might not be available right away.
            // If that turns out to be the case, consider wrapping this call into some sort of exeponential backoff retry.
            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogKickEntry>
                                        (args.Guild, AuditLogActionType.Kick, (x) => x.Target.Id == args.Member.Id);

            // User left the server on their own
            if (!auditResolveResult.Resolved) return;

            var auditKickEntry = auditResolveResult.Result;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditKickEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            var memberName = args.Member.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"**{memberName}** was kicked by **{responsibleName}**. Reason: {auditKickEntry.Reason}.");
        }

        public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            if (args.Channel.IsPrivate) return;

            var message = await _discordResolver.ResolveMessage(args.Channel, args.Message.Id);

            if (message == null) return;

            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                                (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == message.Id);


            // User deleted their own message
            if (!auditResolveResult.Resolved) return;

            var auditMessageDeleteEntry = auditResolveResult.Result;

            if (message.Author.Id == auditMessageDeleteEntry.UserResponsible.Id) return;

            var authorAsMember = await _discordResolver.ResolveGuildMember(args.Guild, message.Author.Id);

            if (authorAsMember == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            var authorName = authorAsMember.GetNicknameOrDisplayName();

            await _discordLogger.LogMessage($"Message written by **{authorName}** in **{args.Channel.Name}** was removed by **{responsibleName}**.");
        }

        public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var messages = args.Messages;
            var author = args.Messages.FirstOrDefault()?.Author;

            if (author == null) return;

            var auditMessageDeleteEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                            (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == author.Id);

            if (auditMessageDeleteEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            var authorName = author.GetUserFullUsername();

            await _discordLogger.LogMessage($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");
        }
    }
}
