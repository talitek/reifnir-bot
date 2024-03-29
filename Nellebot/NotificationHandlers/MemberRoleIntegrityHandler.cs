using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;

namespace Nellebot.NotificationHandlers;

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

        var memberRoleIds = _options.MemberRoleIds;
        var memberRoleId = _options.MemberRoleId;

        var member = notification.EventArgs.Member;

        if (member is null) throw new Exception(nameof(member));

        var memberRole = notification.EventArgs.Guild.Roles[memberRoleId];

        if (memberRole is null)
        {
            throw new ArgumentException($"Could not find role with id {memberRoleId}");
        }

        var userShouldHaveMemberRole = member.Roles.Any(r => memberRoleIds.Contains(r.Id));
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
