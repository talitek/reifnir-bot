using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using Nellebot.Services.Loggers;
using Quartz;

namespace Nellebot.Jobs;

public class RoleMaintenanceJob : IJob
{
    public static readonly JobKey Key = new("role-maintenance", "default");

    private readonly DiscordClient _client;
    private readonly DiscordLogger _discordLogger;
    private readonly BotOptions _options;

    public RoleMaintenanceJob(IOptions<BotOptions> options, DiscordClient client, DiscordLogger discordLogger)
    {
        _client = client;
        _discordLogger = discordLogger;
        _options = options.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _discordLogger.LogExtendedActivityMessage($"Job started: {Key}");

            CancellationToken cancellationToken = context.CancellationToken;

            ulong guildId = _options.GuildId;
            ulong memberRoleId = _options.MemberRoleId;
            ulong[] memberRoleIds = _options.MemberRoleIds;
            ulong ghostRoleId = _options.GhostRoleId;

            DiscordGuild guild = _client.Guilds[guildId];

            DiscordRole? memberRole = guild.Roles[memberRoleId];
            DiscordRole? ghostRole = guild.Roles[ghostRoleId];

            if (memberRole is null)
            {
                throw new ConfigurationErrorsException($"Could not find member role with id {memberRoleId}");
            }

            if (ghostRole is null)
            {
                throw new ConfigurationErrorsException($"Could not find ghost role with id {ghostRoleId}");
            }

            await AddMissingMemberRoles(guild, memberRoleIds, memberRole, cancellationToken);

            await RemoveUnneededMemberRoles(guild, memberRoleIds, memberRole, cancellationToken);

            await AddMissingGhostRoles(guild, ghostRole, cancellationToken);

            await RemoveUnneededGhostRoles(guild, ghostRole, cancellationToken);

            _discordLogger.LogExtendedActivityMessage($"Job finished: {Key}");
        }
        catch (Exception e)
        {
            throw new JobExecutionException(e);
        }
    }

    private async Task AddMissingMemberRoles(
        DiscordGuild guild,
        ulong[] memberRoleIds,
        DiscordRole memberRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> missingMemberRoleMembers = guild.Members
            .Select(m => m.Value)
            .Where(m => m.Roles.All(r => r.Id != memberRole.Id))
            .Where(m => m.Roles.Any(r => memberRoleIds.Contains(r.Id)))
            .ToList();

        if (missingMemberRoleMembers.Count != 0)
        {
            int totalCount = missingMemberRoleMembers.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {missingMemberRoleMembers.Count} users which are missing the Member role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                missingMemberRoleMembers,
                m => m.GrantRoleAsync(memberRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done adding Member role for {successCount}/{totalCount} users.");
        }
    }

    private async Task RemoveUnneededMemberRoles(
        DiscordGuild guild,
        ulong[] memberRoleIds,
        DiscordRole memberRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> memberRoleCandidates = guild.Members
            .Select(m => m.Value)
            .Where(
                m => !m.Roles.Any(r => memberRoleIds.Contains(r.Id))
                     && m.Roles.Any(r => r.Id == memberRole.Id))
            .ToList();

        if (memberRoleCandidates.Count != 0)
        {
            int totalCount = memberRoleCandidates.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {memberRoleCandidates.Count} users with unneeded Member role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                memberRoleCandidates,
                m => m.RevokeRoleAsync(memberRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done removing Member role for {successCount}/{totalCount} users.");
        }
    }

    private async Task AddMissingGhostRoles(
        DiscordGuild guild,
        DiscordRole ghostRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> ghostRoleCandidates = guild.Members
            .Select(r => r.Value)
            .Where(m => !m.Roles.Any())
            .ToList();

        if (ghostRoleCandidates.Count != 0)
        {
            int totalCount = ghostRoleCandidates.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {ghostRoleCandidates.Count} users which are missing the Ghost role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                ghostRoleCandidates,
                m => m.GrantRoleAsync(ghostRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done adding Ghost role for {successCount}/{totalCount} users.");
        }
    }

    private async Task RemoveUnneededGhostRoles(
        DiscordGuild guild,
        DiscordRole ghostRole,
        CancellationToken cancellationToken)
    {
        List<DiscordMember> ghostRoleCandidates = guild.Members
            .Select(r => r.Value)
            .Where(m => m.Roles.Any(r => r.Id == ghostRole.Id) && m.Roles.Count() > 1)
            .ToList();

        if (ghostRoleCandidates.Count != 0)
        {
            int totalCount = ghostRoleCandidates.Count;

            _discordLogger.LogExtendedActivityMessage(
                $"Found {ghostRoleCandidates.Count} users with unneeded Ghost role.");

            int successCount = await ExecuteRoleChangeWithRetry(
                ghostRoleCandidates,
                m => m.RevokeRoleAsync(ghostRole),
                cancellationToken);

            _discordLogger.LogExtendedActivityMessage(
                $"Done removing Ghost role for {successCount}/{totalCount} users.");
        }
    }

#pragma warning disable SA1204
    private static async Task<int> ExecuteRoleChangeWithRetry(
        List<DiscordMember> roleRecipients,
        Func<DiscordMember, Task> roleChangeFunc,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        const int roleChangeDelayMs = 100;
        const int retryDelayMs = 1000;

        foreach (DiscordMember member in roleRecipients)
        {
            var attempt = 0;

            while (attempt < 3)
            {
                attempt++;

                try
                {
                    await roleChangeFunc(member);

                    successCount++;

                    break;
                }
                catch (Exception)
                {
                    await Task.Delay(retryDelayMs * attempt, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(roleChangeDelayMs, cancellationToken);
        }

        return successCount;
    }
}
