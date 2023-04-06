using System;
using System.Threading.Tasks;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;

namespace Nellebot.Services;

public class BotSettingsService
{
    private readonly BotSettingsRepository _botSettingsRepo;
    private readonly SharedCache _cache;

    private static readonly string GreetingMessageKey = "GreetingMessage";
    private static readonly string GreetingMessageUserVariable = "$USER";
    private static readonly string LastHeartbeatKey = "LastHeartbeat";

    public BotSettingsService(BotSettingsRepository botSettingsRepos, SharedCache cache)
    {
        _botSettingsRepo = botSettingsRepos;
        _cache = cache;
    }

    public async Task SetGreetingMessage(string message)
    {
        await _botSettingsRepo.SaveBotSetting(GreetingMessageKey, message);

        _cache.FlushCache(SharedCacheKeys.GreetingMessage);
    }

    public async Task<string?> GetGreetingsMessage(string userMention)
    {
        var messageTemplate = await _cache.LoadFromCacheAsync(
            SharedCacheKeys.GreetingMessage,
            () => _botSettingsRepo.GetBotSetting(SharedCacheKeys.GreetingMessage),
            TimeSpan.FromMinutes(5));

        if (messageTemplate == null) return null;

        var message = messageTemplate.Replace(GreetingMessageUserVariable, userMention);

        return message;
    }

    public Task SetLastHeartbeat(DateTimeOffset heartbeatDateTime)
    {
        return _botSettingsRepo.SaveBotSetting(LastHeartbeatKey, heartbeatDateTime.ToUnixTimeMilliseconds().ToString());
    }

    public async Task<DateTimeOffset?> GetLastHeartbeat()
    {
        var lastHeartBeatStringValue = await _botSettingsRepo.GetBotSetting(LastHeartbeatKey);

        if (lastHeartBeatStringValue == null) return null;

        var parsed = long.TryParse(lastHeartBeatStringValue, out var lastHeartBeatTicks);

        if (!parsed) return null;

        return DateTimeOffset.FromUnixTimeMilliseconds(lastHeartBeatTicks);
    }
}
