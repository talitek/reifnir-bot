using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Entities.AuditLogs;
using Nellebot.Services.Loggers;

namespace Nellebot.Utils
{
    public class DiscordResolver
    {
        private readonly IDiscordErrorLogger _discordErrorLogger;
        private readonly DiscordClient _client;

        public DiscordResolver(IDiscordErrorLogger discordErrorLogger, DiscordClient client)
        {
            _discordErrorLogger = discordErrorLogger;
            _client = client;
        }

        public DiscordGuild ResolveGuild()
        {
            return _client.Guilds.Select(x => x.Value).First();
        }

        public TryResolveResult<DiscordRole> TryResolveRoleByName(DiscordGuild guild, string discordRoleName)
        {
            var matchingDiscordRoles = guild.Roles
                             .Where(kv => kv.Value.Name.Contains(discordRoleName, StringComparison.OrdinalIgnoreCase))
                             .ToList();

            if (matchingDiscordRoles.Count == 0)
            {
                return TryResolveResult<DiscordRole>.FromError($"No role matches the name {discordRoleName}");
            }
            else if (matchingDiscordRoles.Count > 1)
            {
                return TryResolveResult<DiscordRole>.FromError($"More than 1 role matches the name {discordRoleName}");
            }

            var discordRole = matchingDiscordRoles[0].Value;

            return TryResolveResult<DiscordRole>.FromValue(discordRole);
        }

        public DiscordThreadChannel? ResolveThread(ulong threadId)
        {
            var guild = ResolveGuild();

            var threadExists = guild.Threads.TryGetValue(threadId, out var discordThreadChannel);

            if (threadExists) return discordThreadChannel;

            _discordErrorLogger.LogError($"Couldn't resolve thread with id {threadId}");

            return null;
        }

        public DiscordChannel? ResolveChannel(ulong channelId)
        {
            var guild = ResolveGuild();

            var channelExists = guild.Channels.TryGetValue(channelId, out var discordChannel);

            if (channelExists) return discordChannel;

            _discordErrorLogger.LogError($"Couldn't resolve channel with id {channelId}");

            return null;
        }

        public async Task<DiscordChannel?> ResolveChannelAsync(ulong channelId)
        {
            var guild = ResolveGuild();

            var channelExists = guild.Channels.TryGetValue(channelId, out var discordChannel);

            if (channelExists) return discordChannel;

            try
            {
                return (await guild.GetChannelsAsync()).Single(x => x.Id == channelId);
            }
            catch (Exception ex)
            {
                _discordErrorLogger.LogError(ex, "Missing channel");

                return null;
            }
        }

        [Obsolete("Use ResolveChannel(ulong) instead")]
        public Task<DiscordChannel?> ResolveChannel(DiscordGuild guild, ulong channelId)
        {
            return Task.FromResult(ResolveChannel(channelId));
        }

        [Obsolete("Use ResolveChannel(ulong) instead")]
        public Task<DiscordChannel?> ResolveChannel(ulong guildId, ulong channelId)
        {
            return Task.FromResult(ResolveChannel(channelId));
        }

        public Task<DiscordMember?> ResolveGuildMember(ulong userId)
        {
            return ResolveGuildMember(ResolveGuild(), userId);
        }

        public async Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
        {
            var memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

            if (memberExists) return member;

            try
            {
                return (await guild.GetMemberAsync(userId)) ?? throw new ArgumentException($"Missing member with id {userId}");
            }
            catch (Exception ex)
            {
                _discordErrorLogger.LogError(ex, "Missing member");

                return null;
            }
        }

        public async Task<DiscordMessage?> ResolveMessage(DiscordChannel channel, ulong messageId)
        {
            try
            {
                return (await channel.GetMessageAsync(messageId)) ?? throw new ArgumentException($"Missing message with id {messageId}");
            }
            catch (Exception ex)
            {
                _discordErrorLogger.LogError(ex, "Missing message");

                return null;
            }
        }

        public async Task<TryResolveResult<DiscordMessage>> TryResolveMessage(DiscordChannel channel, ulong messageId)
        {
            try
            {
                var message = await channel.GetMessageAsync(messageId);

                return TryResolveResult<DiscordMessage>.FromValue(message);
            }
            catch (Exception)
            {
                return TryResolveResult<DiscordMessage>.FromError("Message not found");
            }
        }

        public async Task<T?> ResolveAuditLogEntry<T>(DiscordGuild guild, DiscordAuditLogActionType logType, Func<T, bool> predicate)
            where T : DiscordAuditLogEntry
        {
            await foreach (var entry in guild.GetAuditLogsAsync(limit: 50, byMember: null!, actionType: logType))
            {
                if (entry is T tEntry && predicate(tEntry))
                {
                    return tEntry;
                }
            }

            return null;
        }

        public async Task<TryResolveResult<T>> TryResolveAuditLogEntry<T>(DiscordGuild guild, DiscordAuditLogActionType logType, Func<T, bool> predicate, int maxAgeMinutes = 1)
            where T : DiscordAuditLogEntry
        {
            await foreach (var entry in guild.GetAuditLogsAsync(limit: 50, byMember: null!, actionType: logType))
            {
                if (entry.CreationTimestamp < DateTimeOffset.UtcNow.AddMinutes(-maxAgeMinutes)) continue;

                if (entry is T tEntry && predicate(tEntry))
                {
                    return TryResolveResult<T>.FromValue(tEntry);
                }
            }

            return TryResolveResult<T>.FromError("Audit log entry not found");
        }
    }
}
