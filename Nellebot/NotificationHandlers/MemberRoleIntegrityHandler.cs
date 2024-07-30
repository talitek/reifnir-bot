using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot.Jobs;
using Quartz;

namespace Nellebot.NotificationHandlers;

public class MemberRoleIntegrityHandler : INotificationHandler<GuildMemberUpdatedNotification>
{
    private readonly ILogger<MemberRoleIntegrityHandler> _logger;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly BotOptions _options;

    public MemberRoleIntegrityHandler(
        ILogger<MemberRoleIntegrityHandler> logger,
        IOptions<BotOptions> options,
        ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
        _options = options.Value;
    }

    public async Task Handle(GuildMemberUpdatedNotification notification, CancellationToken cancellationToken)
    {
        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        IReadOnlyCollection<IJobExecutionContext> jobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
        bool roleMaintenanceIsRunning = jobs.Any(j => Equals(j.JobDetail.Key, RoleMaintenanceJob.Key));

        if (roleMaintenanceIsRunning)
        {
            _logger.LogDebug("Role maintenance job is currently running, skipping role integrity check");
            return;
        }

        bool memberRolesChanged = notification.EventArgs.RolesBefore.Count != notification.EventArgs.RolesAfter.Count;

        if (!memberRolesChanged) return;

        ulong[] memberRoleIds = _options.MemberRoleIds;
        ulong memberRoleId = _options.MemberRoleId;

        DiscordMember? member = notification.EventArgs.Member;

        if (member is null) throw new Exception(nameof(member));

        DiscordRole? memberRole = notification.EventArgs.Guild.Roles[memberRoleId];

        if (memberRole is null)
        {
            throw new ArgumentException($"Could not find role with id {memberRoleId}");
        }

        bool userShouldHaveMemberRole = member.Roles.Any(r => memberRoleIds.Contains(r.Id));
        bool userHasMemberRole = member.Roles.Any(r => r.Id == memberRoleId);

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
