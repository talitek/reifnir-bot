using DSharpPlus.Entities;
using MediatR;
using Nellebot.Common.AppDiscordModels;
using Nellebot.Data.Repositories;
using Nellebot.DiscordModelMappers;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class ActivityLogHandler : INotificationHandler<GuildBanAddedNotification>,
                                        INotificationHandler<GuildBanRemovedNotification>,
                                        INotificationHandler<MessageDeletedNotification>,
                                        INotificationHandler<MessageBulkDeletedNotification>,
                                        INotificationHandler<GuildMemberAddedNotification>,
                                        INotificationHandler<GuildMemberRemovedNotification>,
                                        INotificationHandler<GuildMemberUpdatedNotification>,
                                        INotificationHandler<PresenceUpdatedNotification>
    {
        private readonly DiscordLogger _discordLogger;
        private readonly IDiscordErrorLogger _discordErrorLogger;
        private readonly DiscordResolver _discordResolver;
        private readonly MessageRefRepository _messageRefRepo;

        public ActivityLogHandler(DiscordLogger discordLogger, IDiscordErrorLogger discordErrorLogger, DiscordResolver discordResolver, MessageRefRepository messageRefRepo)
        {
            _discordLogger = discordLogger;
            _discordErrorLogger = discordErrorLogger;
            _discordResolver = discordResolver;
            _messageRefRepo = messageRefRepo;
        }

        public async Task Handle(GuildBanAddedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberName = args.Member.GetNicknameOrDisplayName();
            var memberIdentifier = args.Member.GetDetailedMemberIdentifier();

            var auditBanEntry = await _discordResolver.ResolveAuditLogEntry<DiscordAuditLogBanEntry>
                                    (args.Guild, AuditLogActionType.Ban, (x) => x.Target.Id == args.Member.Id);

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
                                    (args.Guild, AuditLogActionType.Unban, (x) => x.Target.Id == args.Member.Id);

            if (auditUnbanEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditUnbanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogActivityMessage($"**{memberName}** was unbanned by **{responsibleName}**. [{memberIdentifier}]");
        }

        public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var guild = args.Guild;
            var channel = args.Channel;
            var deletedMessage = args.Message;

            if (channel.IsPrivate) return;

            var message = await ResolveMessage(deletedMessage);

            if (message == null)
            {
                await _discordErrorLogger.LogWarning($"{nameof(MessageDeletedNotification)}", $"Could not resolve message id {deletedMessage.Id}");

                await _discordLogger.LogExtendedActivityMessage($"An unknown message in **{channel.Name}** was removed");

                return;
            }

            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                                            // the target is supposed to be a Message but the id corresponds to a user
                                                            (guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == message.Author.Id);

            // User deleted their own message
            if (!auditResolveResult.Resolved) return;

            var auditMessageDeleteEntry = auditResolveResult.Value;

            var authorAsMember = await _discordResolver.ResolveGuildMember(guild, message.Author.Id);

            var memberResponsible = await _discordResolver.ResolveGuildMember(guild, auditMessageDeleteEntry.UserResponsible.Id);

            var responsibleName = memberResponsible?.GetNicknameOrDisplayName() ?? "Unknown mod";

            var authorName = authorAsMember?.GetNicknameOrDisplayName() ?? "Unknown user";
            var authorMention = authorAsMember?.Mention ?? "Unknow user";

            await _discordLogger.LogActivityMessage(
                $"Message written by **{authorName}** in **{channel.Name}** was removed by **{responsibleName}**.");

            var extendedMessage = $"Message written by **{authorMention}** in **{channel.Name}** was removed by **{responsibleName}**.";

            if (!string.IsNullOrWhiteSpace(message.Content))
                extendedMessage += $" Original message:{Environment.NewLine}> {message.Content}";

            await _discordLogger.LogExtendedActivityMessage(extendedMessage);
        }

        // TODO handle bulk deletion of messages belonging to different authors
        public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            if (args.Messages.Count == 0) return;

            var messages = (await Task.WhenAll(args.Messages.Select(ResolveMessage))).ToList();

            var author = args.Messages.Where(m => m?.Author != null).Select(m => m.Author).FirstOrDefault();

            if (author == null)
            {
                await _discordErrorLogger.LogWarning($"{nameof(MessageBulkDeletedNotification)}", $"Could not find any message authors");

                await _discordLogger.LogExtendedActivityMessage($"{messages.Count} unknown messages were removed.");

                return;
            }

            var authorName = author.GetUserFullUsername();

            var auditResolveResult = await _discordResolver.TryResolveAuditLogEntry<DiscordAuditLogMessageEntry>
                                (args.Guild, AuditLogActionType.MessageDelete, (x) => x.Target.Id == author.Id);

            if (!auditResolveResult.Resolved) return;

            var auditMessageDeleteEntry = auditResolveResult.Value;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            var responsibleName = memberResponsible?.GetNicknameOrDisplayName() ?? "Unknown mod";

            await _discordLogger.LogActivityMessage($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

            var sb = new StringBuilder();

            sb.AppendLine($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");

            foreach (var message in messages.Where(x => x != null).Cast<AppDiscordMessage>())
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
                var auditKickEntry = auditResolveResult.Value;

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

        private async Task<AppDiscordMessage?> ResolveMessage(DiscordMessage deletedMessage)
        {
            if (deletedMessage == null) return null;

            var appDiscordMessage = DiscordMessageMapper.Map(deletedMessage);

            var isCompletedMessage = !string.IsNullOrWhiteSpace(appDiscordMessage.Content) && appDiscordMessage.Author != null;

            if (isCompletedMessage) return appDiscordMessage;

            var messageRef = await _messageRefRepo.GetMessageRef(deletedMessage.Id);

            if (messageRef == null) return null;

            appDiscordMessage.Author = new AppDiscordMember() { Id = messageRef.UserId };

            return appDiscordMessage;
        }
    }
}
