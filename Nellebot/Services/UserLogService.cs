using Microsoft.Extensions.Options;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Nellebot.Services;

public class UserLogService
{
    private readonly UserLogRepository _userLogRepo;
    private readonly SharedCache _cache;
    private readonly BotOptions _options;

    public UserLogService(UserLogRepository userLogRepo, SharedCache cache, IOptions<BotOptions> options)
    {
        _userLogRepo = userLogRepo;
        _cache = cache;
        _options = options.Value;
    }

    public Task<UserLog?> GetLatestFieldForUser(ulong userId, UserLogType logType)
    {
        var cacheKey = string.Format(SharedCacheKeys.UserLog, userId, logType);

        var userLog = _cache.LoadFromCacheAsync(cacheKey,
                        () => _userLogRepo.GetLatestFieldForUser(userId, logType),
                        TimeSpan.FromMinutes(5));

        return userLog;
    }

    public async Task CreateUserLog<T>(ulong userId, T value, UserLogType logType, ulong? responsibleUserId = null)
    {
        // Likely temporary
        if (!_options.AutoCreateUserLogsEnabled) return;

        var cacheKey = string.Format(SharedCacheKeys.UserLog, userId, logType);

        await _userLogRepo.CreateUserLog(userId, value, logType, responsibleUserId);

        _cache.FlushCache(cacheKey);
    }

}
