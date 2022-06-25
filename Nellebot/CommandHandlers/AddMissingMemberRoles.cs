using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers
{
    public class AddMissingMemberRoles
    {
        public class AddMissingMemberRolesRequest : CommandRequest
        {
            public ulong MemberRoleId { get; set; }
            public uint ProficiencyGroup { get; set; }

            public AddMissingMemberRolesRequest(CommandContext ctx) : base(ctx)
            {

            }
        }

        public class AddMissingMembeRolesHandler : AsyncRequestHandler<AddMissingMemberRolesRequest>
        {
            private readonly RoleService _roleService;

            public AddMissingMembeRolesHandler(RoleService roleService)
            {
                _roleService = roleService;
            }

            protected override async Task Handle(AddMissingMemberRolesRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;

                var memberRole = ctx.Guild.Roles[request.MemberRoleId];

                if (memberRole == null)
                    throw new ArgumentException($"Could not find role with id {request.MemberRoleId}");

                var proficiencyRoleIds = (await _roleService.GetUserRolesByGroup(request.ProficiencyGroup)).Select(r => r.RoleId);

                var memberRoleCandidates = ctx.Guild.Members.Where(m => m.Value.Roles.Any(r => proficiencyRoleIds.Contains(r.Id)))
                                            .Select(r => r.Value)
                                            .ToList();

                await ctx.RespondAsync($"Found {memberRoleCandidates.Count} candidates");

                var totalCount = 0;
                var successCount = 0;
                var failureCount = 0;
                var progressPercentLastUpdate = 0.0;
                const int baseSleepInMs = 1000;

                foreach (var member in memberRoleCandidates)
                {
                    var roleAddAttempt = 0;
                    var roleAdded = false;

                    while (roleAddAttempt < 3)
                    {
                        roleAddAttempt++;

                        try
                        {
                            await member.GrantRoleAsync(memberRole);

                            roleAdded = true;

                            break;
                        }
                        catch (Exception ex)
                        {
                            await Task.Delay(baseSleepInMs * roleAddAttempt);
                        }
                    }

                    if (roleAdded) successCount++; 
                    else failureCount++;

                    totalCount++;

                    var currentProgress = ((double)totalCount / memberRoleCandidates.Count) * 100;

                    if((currentProgress - progressPercentLastUpdate >= 10) || (currentProgress == 100))
                    {
                        progressPercentLastUpdate = currentProgress;
                        await ctx.Channel.SendMessageAsync($"Progress: {currentProgress}%");
                    }

                    await Task.Delay(baseSleepInMs);
                }

                await ctx.Channel.SendMessageAsync($"Done adding member roles for {successCount}/{totalCount} users.");
            }
        }
    }
}
