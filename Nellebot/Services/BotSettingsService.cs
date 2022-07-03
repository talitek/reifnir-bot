using Nellebot.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class BotSettingsService
    {
        private readonly BotSettingsRepository _botSettingsRepo;
        private readonly SharedCache _cache;

        private static readonly string GreetingMessage = "GreetingMessage";
        private static readonly string GreetingMessageUserVariable = "$USER";

        public BotSettingsService(
            BotSettingsRepository botSettingsRepos,
            SharedCache cache
            )
        {
            _botSettingsRepo = botSettingsRepos;
            _cache = cache;
        }

        public async Task SetGreetingMessage(string message)
        {
            await _botSettingsRepo.SaveBotSetting(GreetingMessage, message);

            _cache.FlushCache(SharedCacheKeys.GreetingMessage);
        }

        public async Task<string?> GetGreetingsMessage(string userMention)
        {
            var messageTemplate = await _cache.LoadFromCacheAsync(SharedCacheKeys.GreetingMessage, async () =>
                await _botSettingsRepo.GetBotSetting(SharedCacheKeys.GreetingMessage),
                TimeSpan.FromMinutes(10));

            if(messageTemplate == null) return null;

            var message = messageTemplate.Replace(GreetingMessageUserVariable, userMention);

            return message;
        }
    }
}
