using Nellebot.Data;
using Nellebot.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class GuildSettingsService
    {
        private readonly GuildSettingsRepository _guildSettingsRepo;
        private readonly SharedCache _cache;

        private static readonly string BotChannel = "BotChannel";

        public GuildSettingsService(
            GuildSettingsRepository guildSettingsRepo,
            SharedCache cache
            )
        {
            _guildSettingsRepo = guildSettingsRepo;
            _cache = cache;
        }

        public async Task SetBotChannel(ulong guildId, string channelType, ulong channelId)
        {
            await _guildSettingsRepo.SaveGuildSetting(guildId, channelType, channelId.ToString());

            var cacheKey = string.Format(SharedCacheKeys.BotChannel, guildId);

            _cache.FlushCache(cacheKey);
        }

        public async Task<ulong?> GetBotChannelId(ulong guildId, string channelType)
        {
            var cacheKey = string.Format(SharedCacheKeys.BotChannel, guildId, channelType);

            var channelId = await _cache.LoadFromCacheAsync(cacheKey, async () =>
                await _guildSettingsRepo.GetGuildSetting(guildId, channelType),
                TimeSpan.FromMinutes(10));

            var parsed = ulong.TryParse(channelId, out var result);

            return parsed ? (ulong?)result : null;
        }
    }

    public static class BotChannelMap
    {
        public const string Commands = "BotCommandsChannel";
        public const string Log = "BotLogChannel";
    }
}
