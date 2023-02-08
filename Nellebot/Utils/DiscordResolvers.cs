using DSharpPlus;
using DSharpPlus.Entities;
using Nellebot.Services.Loggers;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public Task<DiscordChannel?> ResolveChannel(DiscordGuild guild, ulong channelId)
        {
            var channelExists = guild.Channels.TryGetValue(channelId, out var discordChannel);

            if (channelExists) return Task.FromResult(discordChannel);

            try
            {
                var channel = guild.GetChannel(channelId) ?? throw new ArgumentException($"Missing channel with id {channelId}");

                return Task.FromResult<DiscordChannel?>(guild.GetChannel(channelId));
            }
            catch (Exception ex)
            {
                _discordErrorLogger.LogError(ex, "Missing channel");

                return Task.FromResult<DiscordChannel?>(null);
            }
        }

        public async Task<DiscordChannel?> ResolveChannel(ulong guildId, ulong channelId)
        {
            var guild = _client.Guilds[guildId];

            return await ResolveChannel(guild, channelId);
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
                return (await channel.GetMessageAsync(messageId)) ?? throw new ArgumentException($"Missing message with id {messageId}"); ;
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

        public async Task<T?> ResolveAuditLogEntry<T>(DiscordGuild guild, AuditLogActionType logType, Func<T, bool> predicate) where T : DiscordAuditLogEntry
        {
            var entry = (await guild.GetAuditLogsAsync(limit: 50, by_member: null, action_type: logType))
                .Cast<T>()
                .FirstOrDefault(predicate);

            if (entry == null)
            {
                _discordErrorLogger.LogError("Missing audit entry", $"Missing audit entry of type {logType}");
                return null;
            }

            return entry;
        }

        public async Task<TryResolveResult<T>> TryResolveAuditLogEntry<T>(DiscordGuild guild, AuditLogActionType logType, Func<T, bool> predicate) where T : DiscordAuditLogEntry
        {
            var entry = (await guild.GetAuditLogsAsync(limit: 50, by_member: null, action_type: logType))
                .Where(x => x.CreationTimestamp > DateTimeOffset.UtcNow.AddMinutes(-1))
                .Cast<T>()
                .FirstOrDefault(predicate);

            if (entry == null)
            {
                return TryResolveResult<T>.FromError("Audit log entry not found");
            }

            return TryResolveResult<T>.FromValue(entry);
        }
    }
}
