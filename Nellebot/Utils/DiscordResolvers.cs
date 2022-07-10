using DSharpPlus;
using DSharpPlus.Entities;
using Nellebot.Services.Loggers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public TryResolveResult TryResolveRoleByName(DiscordGuild guild, string discordRoleName, out DiscordRole discordRole)
        {
            var matchingDiscordRoles = guild.Roles
                             .Where(kv => kv.Value.Name.Contains(discordRoleName, StringComparison.OrdinalIgnoreCase))
                             .ToList();

            if (matchingDiscordRoles.Count == 0)
            {
                discordRole = null!;
                return new TryResolveResult(false, $"No role matches the name {discordRoleName}");
            }
            else if (matchingDiscordRoles.Count > 1)
            {
                discordRole = null!;
                return new TryResolveResult(false, $"More than 1 role matches the name {discordRoleName}");
            }

            discordRole = matchingDiscordRoles[0].Value;

            return new TryResolveResult(true);
        }

        public async Task<DiscordChannel?> ResolveChannel(DiscordGuild guild, ulong channelId)
        {
            var channelExists = guild.Channels.TryGetValue(channelId, out var discordChannel);

            if (channelExists) return discordChannel;

            try
            {
                return guild.GetChannel(channelId) ?? throw new ArgumentException($"Missing channel with id {channelId}");
            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogError("Missing channel", ex.ToString());

                return null;
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
                await _discordErrorLogger.LogError("Missing member", ex.ToString());

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
                await _discordErrorLogger.LogError("Missing message", ex.ToString());

                return null;
            }
        }

        public async Task<TryResolveResultObject<DiscordMessage>> TryResolveMessage(DiscordChannel channel, ulong messageId)
        {
            try
            {
                var message = await channel.GetMessageAsync(messageId);

                return new TryResolveResultObject<DiscordMessage>(message);
            }
            catch (Exception)
            {
                return new TryResolveResultObject<DiscordMessage>(false, "Message not found");
            }
        }

        public async Task<T?> ResolveAuditLogEntry<T>(DiscordGuild guild, AuditLogActionType logType, Func<T, bool> predicate) where T : DiscordAuditLogEntry
        {
            var entry = (await guild.GetAuditLogsAsync(limit: 50, by_member: null, action_type: logType))
                .Cast<T>()
                .FirstOrDefault(predicate);

            if (entry == null)
            {
                await _discordErrorLogger.LogError("Missing audit entry", $"Missing audit entry of type {logType}");
                return null;
            }

            return entry;
        }
    }
}
