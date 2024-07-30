using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Options;

namespace Nellebot.CommandHandlers;

public record AddMissingMemberRolesCommand : BotCommandCommand
{
    public AddMissingMemberRolesCommand(CommandContext ctx)
        : base(ctx)
    { }
}

public class AddMissingMembeRolesHandler : IRequestHandler<AddMissingMemberRolesCommand>
{
    private readonly BotOptions _options;

    public AddMissingMembeRolesHandler(IOptions<BotOptions> options)
    {
        _options = options.Value;
    }

    public async Task Handle(AddMissingMemberRolesCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;

        ulong memberRoleId = _options.MemberRoleId;
        ulong[] memberRoleIds = _options.MemberRoleIds;

        DiscordRole? memberRole = ctx.Guild.Roles[_options.MemberRoleId];

        if (memberRole == null)
        {
            throw new ArgumentException($"Could not find role with id {memberRoleId}");
        }

        List<DiscordMember> memberRoleCandidates = ctx.Guild.Members
            .Where(m => !m.Value.Roles.Any(r => r.Id == memberRoleId))
            .Where(m => m.Value.Roles.Any(r => memberRoleIds.Contains(r.Id)))
            .Select(r => r.Value)
            .ToList();

        if (memberRoleCandidates.Count == 0)
        {
            await ctx.RespondAsync("Did not find any candidates for member role");
            return;
        }

        await ctx.RespondAsync($"Found {memberRoleCandidates.Count} candidates for member role");

        var totalCount = 0;
        var successCount = 0;
        var failureCount = 0;
        var progressPercentLastUpdate = 0.0;
        const int baseSleepInMs = 1000;

        foreach (DiscordMember member in memberRoleCandidates)
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
                catch (Exception)
                {
                    await Task.Delay(baseSleepInMs * roleAddAttempt, cancellationToken);
                }
            }

            if (roleAdded)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }

            totalCount++;

            double currentProgress = ((double)totalCount / memberRoleCandidates.Count) * 100;

            if (currentProgress - progressPercentLastUpdate >= 10 || currentProgress == 100)
            {
                progressPercentLastUpdate = currentProgress;
                await ctx.Channel.SendMessageAsync($"Progress: {currentProgress:.##}%");
            }

            await Task.Delay(baseSleepInMs, cancellationToken);
        }

        await ctx.Channel.SendMessageAsync($"Done adding member roles for {successCount}/{totalCount} users.");
    }
}
