using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Common.Models.UserLogs;
using Nellebot.Data.Repositories;
using Nellebot.Utils;

namespace Nellebot.CommandHandlers;

public record PopulateUserLogCommand : CommandCommand
{
    public PopulateUserLogCommand(CommandContext ctx)
        : base(ctx) { }
}

public class PopulateUserLogHandler : IRequestHandler<PopulateUserLogCommand>
{
    private readonly UserLogRepository _userLogRepo;

    public PopulateUserLogHandler(UserLogRepository userLogRepo)
    {
        _userLogRepo = userLogRepo;
    }

    public async Task Handle(PopulateUserLogCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var guild = ctx.Guild;
        var channel = ctx.Channel;

        await channel.SendMessageAsync("Fetching users");

        var users = await guild.GetAllMembersAsync();

        await channel.SendMessageAsync($"Fetched {users.Count} users");

        var totalCount = 0;
        var failedCount = 0;
        var progressPercentLastUpdate = 0.0;

        foreach (var user in users)
        {
            try
            {
                var userLogs = await _userLogRepo.GetLatestFieldsForUser(user.Id);

                if (!userLogs.Any(x => x.LogType == UserLogType.JoinedServer))
                    await _userLogRepo.CreateUserLog(user.Id, user.JoinedAt.UtcDateTime, UserLogType.JoinedServer);

                if (!userLogs.Any(x => x.LogType == UserLogType.UsernameChange))
                    await _userLogRepo.CreateUserLog(user.Id, user.GetFullUsername(), UserLogType.UsernameChange);

                if (!userLogs.Any(x => x.LogType == UserLogType.NicknameChange))
                    await _userLogRepo.CreateUserLog(user.Id, user.Nickname, UserLogType.NicknameChange);

                if (!userLogs.Any(x => x.LogType == UserLogType.AvatarHashChange))
                    await _userLogRepo.CreateUserLog(user.Id, user.AvatarHash, UserLogType.AvatarHashChange);

                if (!userLogs.Any(x => x.LogType == UserLogType.GuildAvatarHashChange))
                    await _userLogRepo.CreateUserLog(user.Id, user.GuildAvatarHash, UserLogType.GuildAvatarHashChange);

                totalCount++;

                var currentProgress = ((double)totalCount / users.Count) * 100;

                if ((currentProgress - progressPercentLastUpdate >= 10) || (currentProgress == 100))
                {
                    progressPercentLastUpdate = currentProgress;
                    await ctx.Channel.SendMessageAsync($"Progress: {currentProgress:##}%");
                }
            }
            catch (Exception)
            {
                failedCount++;

                continue;
            }
        }

        await ctx.Channel.SendMessageAsync($"Done populating user logs for {totalCount - failedCount}/{users.Count} users");
    }
}
