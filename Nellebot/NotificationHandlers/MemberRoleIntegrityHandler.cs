using MediatR;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.NotificationHandlers
{
    public class MemberRoleIntegrityHandler : INotificationHandler<GuildMemberUpdatedNotification>
    {
        private readonly BotOptions _options;

        public MemberRoleIntegrityHandler(IOptions<BotOptions> options)
        {
            _options = options.Value;
        }

        public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
        {
            var memberRolesChanged = notification.EventArgs.RolesBefore.Count != notification.EventArgs.RolesAfter.Count;

            if (!memberRolesChanged) return;

            var requiredRoleIds = _options.RequiredRoleIds;
            var memberRoleId = _options.MemberRoleId;

            var member = notification.EventArgs.Member;

            if (member == null) throw new ArgumentNullException(nameof(member));

            var memberRole = notification.EventArgs.Guild.Roles[memberRoleId];

            if (memberRole == null)
                throw new ArgumentException($"Could not find role with id {memberRoleId}");

            var userShouldHaveMemberRole = member.Roles.Any(r => requiredRoleIds.Contains(r.Id));
            var userHasMemberRole = member.Roles.Any(r => r.Id == memberRoleId);

            if (userShouldHaveMemberRole && !userHasMemberRole)
            {
                await member.GrantRoleAsync(memberRole);
            }
            else if (!userShouldHaveMemberRole && userHasMemberRole)
            {
                await member.RevokeRoleAsync(memberRole);
            }
        }
    }
}
