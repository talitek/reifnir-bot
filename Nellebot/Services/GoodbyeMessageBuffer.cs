using System.Collections.Generic;
using System.Linq;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.Services;

public class GoodbyeMessageBuffer
{
    private const int DelayInMs = 5000;
    private const int MaxUsernamesToDisplay = 5;

    private readonly MessageBuffer _buffer;
    private readonly DiscordLogger _discordLogger;

    public GoodbyeMessageBuffer(DiscordLogger discordLogger)
    {
        _discordLogger = discordLogger;

        _buffer = new MessageBuffer(DelayInMs, LogBufferedMessage);
    }

    public void AddUser(string user)
    {
        _buffer.AddMessage(user);
    }

    private void LogBufferedMessage(IEnumerable<string> users)
    {
        var userList = users.ToList();

        if (userList.Count == 0) return;

        if (userList.Count == 1)
        {
            _discordLogger.LogGreetingMessage($"**{userList.First()}** has left the server. Goodbye!");
            return;
        }

        if (userList.Count <= MaxUsernamesToDisplay)
        {
            var userListOutput = string.Join(", ", userList.Select(x => $"**{x}**"));
            _discordLogger.LogGreetingMessage($"The following users have left the server: {userListOutput}. Goodbye!");
            return;
        }

        var usersToShow = userList.Take(MaxUsernamesToDisplay);
        var remainingCount = userList.Count - MaxUsernamesToDisplay;
        var usersToShowOutput = string.Join(", ", usersToShow.Select(x => $"**{x}**"));

        _discordLogger.LogGreetingMessage($"The following users have left the server: {usersToShowOutput} and {remainingCount} others. Goodbye!");
    }
}
