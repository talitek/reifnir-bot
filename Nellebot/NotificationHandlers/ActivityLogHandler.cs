using DSharpPlus.Entities;
using MediatR;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class ActivityLogHandler :   INotificationHandler<GuildBanAddedNotification>,
                                        INotificationHandler<GuildBanRemovedNotification>,
                                        INotificationHandler<MessageDeletedNotification>,
                                        INotificationHandler<MessageBulkDeletedNotification>,
                                        INotificationHandler<GuildMemberAddedNotification>,
                                        INotificationHandler<GuildMemberRemovedNotification>,
                                        INotificationHandler<GuildMemberUpdatedNotification>,
                                        INotificationHandler<PresenceUpdatedNotification>
    {
        private readonly DiscordLogger _discordLogger;
        private readonly DiscordResolver _discordResolver;

        public ActivityLogHandler(DiscordLogger discordLogger, DiscordResolver discordResolver)
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

            await _discordLogger.LogActivityMessage($"**{memberName}** was banned by **{responsibleName}**. Reason: {auditBanEntry.Reason}. [{memberIdentifier}]");
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

            await _discordLogger.LogActivityMessage($"**{memberName}** was unbanned by **{responsibleName}**. [{memberIdentifier}]");
        }

        // Borked
        public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var guild = args.Guild;
            var channel = args.Channel;
            var deletedMessage = args.Message;

            if (channel.IsPrivate) return;

            var message = await _discordResolver.ResolveMessage(channel, deletedMessage.Id);

            if (message == null) return;

            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                                            // the target is supposed to be a Message but the id corresponds to a user
                                                            (guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == message.Author?.Id);

            // User deleted their own message
            if (!auditResolveResult.Resolved) return;

            var auditMessageDeleteEntry = auditResolveResult.Result;

            if (message.Author.Id == auditMessageDeleteEntry.UserResponsible.Id) return;

            var authorAsMember = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

            if (authorAsMember == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            var authorName = authorAsMember.GetNicknameOrDisplayName();
            var authorMention = authorAsMember.Mention;

            await _discordLogger.LogActivityMessage(
                $"Message written by **{authorName}** in **{channel.Name}** was removed by **{responsibleName}**.");

            await _discordLogger.LogExtendedActivityMessage(
                $"Message written by **{authorMention}** in **{channel.Name}** was removed by **{responsibleName}**." 
                + $" Original message:{Environment.NewLine}> {message.Content}");
        }

        // Borked
        public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var messages = args.Messages;

            if (messages.Count == 0) return;

            var author = args.Messages[0].Author;

            if (author == null) return;

            var authorName = author.GetUserFullUsername();

            var auditMessageDeleteEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == author.Id);

            if (auditMessageDeleteEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogActivityMessage($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

            var sb = new StringBuilder();

            sb.AppendLine($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

            foreach (var message in messages)
            {
                sb.AppendLine();
                sb.AppendLine($"In {message.Channel.Name} at {message.CreationTimestamp}:");
                if (!string.IsNullOrWhiteSpace(message.Content)) sb.AppendLine($"> {message.Content}");
            }

            await _discordLogger.LogExtendedActivityMessage(sb.ToString());
        }

        public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var member = args.Member;

            var memberName = member.GetNicknameOrDisplayName();
            var memberFullIdentifier = $"{member.Mention} [{member.GetDetailedMemberIdentifier()}]";

            await _discordLogger.LogActivityMessage($"{memberName} joined the server");
            await _discordLogger.LogExtendedActivityMessage($"{memberFullIdentifier} joined the server");
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var member = args.Member;
            var guild = args.Guild;

            var memberName = member.GetNicknameOrDisplayName();
            var memberFullIdentifier = $"**{member.GetNicknameOrDisplayName()}** [{member.GetDetailedMemberIdentifier()}]";

            // It's possible that the audit log entry might not be available right away.
            // If that turns out to be the case, consider wrapping this call into some sort of exeponential backoff retry.
            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogKickEntry>
                                        (args.Guild, AuditLogActionType.Kick, (x) => x.Target.Id == args.Member.Id);

            var userWasKicked = auditResolveResult.Resolved;

            if (userWasKicked)
            {
                var auditKickEntry = auditResolveResult.Result;

                var memberResponsible = await _discordResolver.ResolveGuildMember(guild, auditKickEntry.UserResponsible.Id);

                if (memberResponsible == null) return;

                var responsibleName = memberResponsible.GetNicknameOrDisplayName();

                await _discordLogger.LogActivityMessage($"**{memberName}** was kicked by **{responsibleName}**. Reason: {auditKickEntry.Reason}.");
                await _discordLogger.LogActivityMessage($"**{memberFullIdentifier}** was kicked by **{responsibleName}**. Reason: {auditKickEntry.Reason}.");
            }
            else
            {
                await _discordLogger.LogActivityMessage($"**{memberName}** left the server");
                await _discordLogger.LogExtendedActivityMessage($"{memberFullIdentifier} left the server");
            }
        }

        // Borked
        public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var removedRole = args.RolesBefore.ExceptBy(args.RolesAfter.Select(r => r.Id), x => x.Id).FirstOrDefault();
            var addedRole = args.RolesAfter.ExceptBy(args.RolesBefore.Select(r => r.Id), x => x.Id).FirstOrDefault();

            var memberMention = args.Member.Mention;

            if (removedRole != null)
            {
                await _discordLogger.LogExtendedActivityMessage($"Role change for {memberMention}: Removed {removedRole.Name}.");
            }
            else if (addedRole != null)
            {
                await _discordLogger.LogExtendedActivityMessage($"Role change for {memberMention}: Added {addedRole.Name}.");
            }

            var nicknameBefore = args.NicknameBefore;
            var nicknameAfter = args.NicknameAfter;

            if (nicknameBefore != nicknameAfter)
            {
                var message = $"Nickname change for {memberMention}.";

                message += string.IsNullOrWhiteSpace(nicknameBefore)
                            ? " No previous nickname."
                            : $" Previous nickname: {nicknameBefore}.";

                await _discordLogger.LogExtendedActivityMessage(message);
            }
        }

        // Borked
        public async Task Handle(PresenceUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var usernameBefore = args.UserBefore?.GetUserFullUsername();
            var usernameAfter = args.UserAfter?.GetUserFullUsername();

            if (string.IsNullOrEmpty(usernameBefore) || string.IsNullOrEmpty(usernameAfter)) return;

            if (usernameBefore != usernameAfter)
            {
                await _discordLogger.LogExtendedActivityMessage($"Username change for {usernameAfter}: Previous username: {usernameBefore}.");
            }
        }
    }
}
