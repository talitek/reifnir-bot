using DSharpPlus.Entities;
using Nellebot.Services;
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

        public DiscordResolver(IDiscordErrorLogger discordErrorLogger)
        {
            _discordErrorLogger = discordErrorLogger;
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
            guild.Channels.TryGetValue(channelId, out var discordChannel);

            if (discordChannel == null)
            {
                try
                {
                    discordChannel = guild.GetChannel(channelId);
                }
                catch (Exception ex)
                {
                    await _discordErrorLogger.LogDiscordError(ex.ToString());

                    return null;
                }
            }

            return discordChannel;
        }

        public async Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
        {
            var memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

            if (memberExists)
                return member;

            try
            {
                return await guild.GetMemberAsync(userId);
            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());

                return null;
            }
        }

        public async Task<DiscordMessage?> ResolveMessage(DiscordChannel channel, ulong messageId)
        {
            try
            {
                var message = await channel.GetMessageAsync(messageId);

                return message;
            }
            catch (Exception ex)
            {
                await _discordErrorLogger.LogDiscordError(ex.ToString());

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
    }
}
