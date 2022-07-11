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
    public class AuditLogHandler : INotificationHandler<GuildMemberUpdatedNotification>,
                                    INotificationHandler<MessageDeletedNotification>,
                                    INotificationHandler<MessageBulkDeletedNotification>,
                                    INotificationHandler<GuildMemberAddedNotification>,
                                    INotificationHandler<GuildMemberRemovedNotification>,
                                    INotificationHandler<PresenceUpdatedNotification>
    {
        private readonly DiscordLogger _discordLogger;
        private readonly DiscordResolver _discordResolver;

        public AuditLogHandler(DiscordLogger discordLogger, DiscordResolver discordResolver)
        {
            _discordLogger = discordLogger;
            _discordResolver = discordResolver;
        }

        public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var removedRole = args.RolesBefore.ExceptBy(args.RolesAfter.Select(r => r.Id), x => x.Id).FirstOrDefault();
            var addedRole = args.RolesAfter.ExceptBy(args.RolesBefore.Select(r => r.Id), x => x.Id).FirstOrDefault();

            var memberMention = args.Member.Mention;

            if (removedRole != null)
            {
                await _discordLogger.LogAuditMessage($"Role change for {memberMention}: Removed {removedRole.Name}.");
            }
            else if (addedRole != null)
            {
                await _discordLogger.LogAuditMessage($"Role change for {memberMention}: Added {addedRole.Name}.");
            }

            var nicknameBefore = args.NicknameBefore;
            var nicknameAfter = args.NicknameAfter;

            if (nicknameBefore != nicknameAfter)
            {
                var message = $"Nickname change for {memberMention}.";

                message += string.IsNullOrWhiteSpace(nicknameBefore)
                            ? " No previous nickname."
                            : $" Previous nickname: {nicknameBefore}.";

                await _discordLogger.LogAuditMessage(message);
            }
        }

        public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            if (args.Channel.IsPrivate) return;

            var message = await _discordResolver.ResolveMessage(args.Channel, args.Message.Id);

            if (message == null) return;

            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                                // the target is supposed to be a Message but the id corresponds to a user
                                                (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == message.Author.Id);

            // User likely deleted their own message
            if (!auditResolveResult.Resolved) return;

            var auditMessageDeleteEntry = auditResolveResult.Result;

            var memberMention = message.Author.Mention;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogAuditMessage(
                $"Message written by **{memberMention}** in **{args.Channel.Name}** was removed by **{responsibleName}**. Original message:{Environment.NewLine}> {message.Content}");
        }

        public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var messages = args.Messages;

            if (messages.Count == 0) return;

            var author = args.Messages[0].Author;

            if (author == null) return;

            var auditMessageDeleteEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == author.Id);

            if (auditMessageDeleteEntry == null) return;

            var authorName = author.GetUserFullUsername();

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            var sb = new StringBuilder($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");
            foreach (var message in messages)
            {
                sb.AppendLine();
                sb.AppendLine($"In {message.Channel.Name} at {message.CreationTimestamp}:");
                if (!string.IsNullOrWhiteSpace(message.Content)) sb.AppendLine($"> {message.Content}");
            }

            await _discordLogger.LogAuditMessage(sb.ToString());
        }

        public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
        {
            var member = notification.EventArgs.Member;

            var memberFullIdentifier = $"{member.Mention} [{member.GetDetailedMemberIdentifier()}]";

            await _discordLogger.LogAuditMessage($"{memberFullIdentifier} joined the server");
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var member = notification.EventArgs.Member;

            var memberFullIdentifier = $"**{member.GetNicknameOrDisplayName()}** [{member.GetDetailedMemberIdentifier()}]";

            await _discordLogger.LogAuditMessage($"{memberFullIdentifier} left the server");
        }

        public async Task Handle(PresenceUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var usernameBefore = args.UserBefore?.GetUserFullUsername();
            var usernameAfter = args.UserAfter?.GetUserFullUsername();

            if (string.IsNullOrEmpty(usernameBefore) || string.IsNullOrEmpty(usernameAfter)) return;

            if (usernameBefore != usernameAfter)
            {
                await _discordLogger.LogAuditMessage($"Username change for {usernameAfter}: Previous username: {usernameBefore}.");
            }
        }
    }
}
