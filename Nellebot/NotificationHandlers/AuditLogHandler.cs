using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
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
    public class AuditLogHandler : INotificationHandler<GuildMemberUpdatedNotification>,
                                    INotificationHandler<MessageDeletedNotification>,
                                    INotificationHandler<MessageBulkDeletedNotification>,
                                    INotificationHandler<GuildMemberAddedNotification>,
                                    INotificationHandler<GuildMemberRemovedNotification>
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

            var oldNickname = args.NicknameBefore;
            var newNickname = args.NicknameAfter;

            if (oldNickname != newNickname)
            {
                await _discordLogger.LogAuditMessage($"Nickname change for {memberMention}: Previous nickname: {oldNickname}.");
            }
        }

        public async Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var message = args.Message;
            var channel = args.Channel;

            if (channel.IsPrivate) return;

            var auditMessageDeleteEntry = await args.Guild
                                            .GetLastAuditLogEntry<DiscordAuditLogMessageEntry>(
                                                AuditLogActionType.MessageDelete, (x) => x.Target.Id == message.Author.Id);

            if (auditMessageDeleteEntry == null) return;

            if (message.Author.Id == auditMessageDeleteEntry.UserResponsible.Id) return;

            var memberMention = message.Author.Mention;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditMessageDeleteEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogAuditMessage(
                $"Message written by **{memberMention}** in **{channel.Name}** was removed by **{responsibleName}**. Original message:{Environment.NewLine}> {message.Content}");
        }

        public async Task Handle(MessageBulkDeletedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var messages = args.Messages;
            var author = args.Messages.FirstOrDefault()?.Author;

            if (author == null) return;

            var auditUnbanEntry = await args.Guild
                        .GetLastAuditLogEntry<DiscordAuditLogBanEntry>(
                            AuditLogActionType.Unban, (x) => x.Target.Id == author.Id);

            if (auditUnbanEntry == null) return;

            var authorName = author.GetUserFullUsername();

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditUnbanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            var sb = new StringBuilder($"{messages.Count} messages written by **{authorName}** were removed by **{responsibleName}**.");
            foreach (var message in messages)
            {
                sb.AppendLine();
                sb.AppendLine($"In {message.Channel.Name} at {message.CreationTimestamp}:");
                sb.AppendLine($"> {message.Content}");
            }

            await _discordLogger.LogAuditMessage(sb.ToString());
        }

        public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
        {
            var member = notification.EventArgs.Member;

            if (member == null) return;

            var memberFullIdentifier = $"**{member.GetNicknameOrDisplayName()}** [{member.GetDetailedMemberIdentifier()}]";

            await _discordLogger.LogAuditMessage($"{memberFullIdentifier} joined the server");
        }

        public async Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
        {
            var member = notification.EventArgs.Member;

            if (member == null) return;

            var memberFullIdentifier = $"**{member.GetNicknameOrDisplayName()}** [{member.GetDetailedMemberIdentifier()}]";

            await _discordLogger.LogAuditMessage($"{memberFullIdentifier} left the server");
        }
    }
}
