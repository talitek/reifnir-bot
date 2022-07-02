using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;
using Nellebot.Utils;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class AuditHandler : INotificationHandler<GuildMemberUpdatedNotification>,
                                INotificationHandler<GuildBanAddedNotification>,
                                INotificationHandler<GuildBanRemovedNotification>
    {
        private readonly DiscordLogger _discordLogger;
        private readonly DiscordResolver _discordResolver;

        public AuditHandler(DiscordLogger discordLogger, DiscordResolver discordResolver)
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
            var memberIdentifier = args.Member.GetDetailedMemberIdentifier();

            if (removedRole != null)
            {
                await _discordLogger.LogAuditMessage($"Role change for {memberMention}: Removed {removedRole.Name}. [{memberIdentifier}]");
            }
            else if (addedRole != null)
            {
                await _discordLogger.LogAuditMessage($"Role change for {memberMention}: Added {addedRole.Name}. [{memberIdentifier}]");
            }

            var oldNickname = args.NicknameBefore;
            var newNickname = args.NicknameAfter;

            if (oldNickname != newNickname)
            {
                await _discordLogger.LogAuditMessage($"Nickname change for {memberMention}: Previous nickname: {oldNickname}. [{memberIdentifier}]");
            }
        }

        public async Task Handle(GuildBanAddedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberMention = args.Member.Mention;

            var auditBanEntry = (await args.Guild.GetAuditLogsAsync(limit: 10, by_member: null, action_type: AuditLogActionType.Ban))
                                .Cast<DiscordAuditLogBanEntry>()
                                .FirstOrDefault(x => x.Target.Id == args.Member.Id);

            if (auditBanEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditBanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogAuditMessage($"{memberMention} was banned by {responsibleName}. Reason: {auditBanEntry.Reason}.");
        }

        public async Task Handle(GuildBanRemovedNotification notification, CancellationToken cancellationToken)
        {
            var args = notification.EventArgs;

            var memberMention = args.Member.Mention;

            var auditUnbanEntry = (await args.Guild.GetAuditLogsAsync(limit: 10, by_member: null, action_type: AuditLogActionType.Unban))
                                    .Cast<DiscordAuditLogBanEntry>()
                                    .FirstOrDefault(x => x.Target.Id == args.Member.Id);

            if (auditUnbanEntry == null) return;

            var memberResponsible = await _discordResolver.ResolveGuildMember(args.Guild, auditUnbanEntry.UserResponsible.Id);

            if (memberResponsible == null) return;

            var responsibleName = memberResponsible.GetNicknameOrDisplayName();

            await _discordLogger.LogAuditMessage($"{memberMention} was unbanned by {responsibleName}.");
        }
    }
}
