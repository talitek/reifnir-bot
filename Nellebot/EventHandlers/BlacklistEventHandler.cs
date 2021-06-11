using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nellebot;
using Nellebot.Helpers;
using Nellebot.Services;
using Nellebot.Utils;
using Nellebot.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.EventHandlers
{
    public class BlacklistEventHandler
    {
        private readonly DiscordClient _client;
        private readonly ILogger<BlacklistEventHandler> _logger;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly BotOptions _options;

        // TODO fetch blacklist from db
        private readonly List<string> HardcodedBlacklist = new List<string>()
        {
            "twitter.com/h0nde",
            "h0nda",
            "h0nde"
        };

        public BlacklistEventHandler(
            DiscordClient client,
            ILogger<BlacklistEventHandler> logger,
            IOptions<BotOptions> options,
            DiscordErrorLogger discordErrorLogger)
        {
            _client = client;
            _logger = logger;
            _discordErrorLogger = discordErrorLogger;
            _options = options.Value;
        }

        public void RegisterHandlers()
        {
            _client.GuildMemberAdded += _client_GuildMemberAdded;
            _client.GuildMemberUpdated += _client_GuildMemberUpdated;
        }

        private async Task _client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs e)
        {
            var guild = e.Guild;
            var updatedUser = e.Member;

            if (MemberHasBlackListName(updatedUser))
            {
                try
                {
                    await e.Guild.BanMemberAsync(e.Member);
                    await LogBlacklistBan(guild, updatedUser, "Blacklisted name");
                }
                catch (Exception ex)
                {
                    await _discordErrorLogger.LogDiscordError(ex.ToString());
                }
            }
        }

        private async Task _client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            var guild = e.Guild;
            var newUser = e.Member;

            if (MemberHasBlackListName(newUser))
            {
                try
                {
                    await e.Guild.BanMemberAsync(e.Member);
                    await LogBlacklistBan(guild, newUser, "Blacklisted name");
                }
                catch (Exception ex)
                {
                    await _discordErrorLogger.LogDiscordError(ex.ToString());
                }
            }
        }

        private bool MemberHasBlackListName(DiscordMember member)
        {
            var username = member.Username;
            var nickname = member.Nickname;

            var blacklist = HardcodedBlacklist.Select(s => s.ToLower()).ToList();

            var isBlacklisted = blacklist.Any(s => username.ToLower().Contains(s))
                                || (!string.IsNullOrWhiteSpace(nickname)
                                    && blacklist.Any(s => nickname.ToLower().Contains(s)));

            return isBlacklisted;
        }

        private async Task LogBlacklistBan(DiscordGuild guild, DiscordMember member, string reason)
        {
            var logChannelId = _options.LogChannelId;

            var logChannel = guild.Channels[logChannelId];

            if (logChannel == null)
            {
                _logger.LogError($"Could not fetch log channel {logChannelId} from guild");
                return;
            }

            await logChannel.SendMessageAsync($"Banned user **{member.GetNicknameOrDisplayName()}**. Reason: {reason}");
        }
    }
}
