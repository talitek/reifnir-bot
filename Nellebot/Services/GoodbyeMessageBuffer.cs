using System;
using System.Collections.Generic;
using System.Linq;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.Services;

public class GoodbyeMessageBuffer
{
    private const int DelayInMs = 5000;
    private const int MaxUsernamesToDisplay = 100;

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
            _discordLogger.LogGreetingMessage(GetRandomGoodbyeMessage(userList.First()));
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

    private string GetRandomGoodbyeMessage(string username)
    {
        var goodbyeMessages = new string[]
        {
            "**{0}** has left. Don't let the door hit you on the way out!",
            "**{0}** has has decided to leave us. We are going to miss you... Not!",
            "**{0}** has left. Was it something we said?",
            "**{0}** has checked out. Guess we were too cool for them!",
            "**{0}** has left. We've already forgotten who they were.",
            "**{0}** has left the building. Anyone notice?",
            "**{0}** has left. The server just got a little lighter!",
            "Oh look, **{0}** couldn't handle the heat!",
            "**{0}** has left. Quick, everyone pretend to be sad!",
            "**{0}** has left. We'll try to hold back our tears.",
            "Did **{0}** just leave? ...Anyway.",
            "**{0}** has left. Wait, who?",
            "Bye **{0}**! We'll save your spot. Just kidding.",
            "**{0}** has left. The server's IQ just went up a notch!",
            "Another one bites the dust! Bye **{0}**!",
            "**{0}** has left. Drama level decreased by 10%!",
            "Oh no! **{0}** left. How will we ever cope? (heavy sarcasm)",
            "**{0}** has left. Guess they couldn't handle our awesomeness!",
            "**{0}** has left. And... life goes on!",
            "There goes **{0}**, off to greener pastures. Or so they think!",
            "**{0}** has left. Guess we weren't their type!",
            "**{0}** has left. Server's charisma just went up!",
            "**{0}** has left. We'll send a postcard!",
            "**{0}** has left. We're all heartbroken. Can't you tell?",
            "Someone sound the alarm! **{0}** has escaped!",
            "**{0}** has left. But we'll always have the memories. Whatever they were.",
            "Bye **{0}**! Don't forget to write. Or do. Whatever.",
            "**{0}** has left. We'll be holding a very brief moment of silence.",
            "**{0}** has left. Server happiness remains unchanged.",
            "**{0}** has left. We'll keep the party going in their honor!",
            "**{0}** has left. But on the bright side, more bandwidth for the rest of us!",
            "And **{0}** is out! Anyone up for a game of 'Remember When **{0}** Was Here?' No? Okay.",
            "**{0}** has left the server. This fills you with determination.",
            "**{0}** has left. Quickly! Lock the door!",
            "**{0}** has left. Hopefully to a galaxy far, far away.",
            "**{0}** has left the server. My day just got better!",
            "**{0}** has decided to leave us. Finally...",
        };

        var idx = new Random().Next(0, goodbyeMessages.Length);

        var formattedGoodbyeMessage = string.Format(goodbyeMessages[idx], username);

        return formattedGoodbyeMessage;
    }
}
