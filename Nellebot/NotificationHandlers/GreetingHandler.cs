using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Nellebot.Common.Models;
using Nellebot.Data.Repositories;
using Nellebot.Infrastructure;
using Nellebot.Services;
using Nellebot.Services.Loggers;

namespace Nellebot.NotificationHandlers;

public class GreetingHandler :
    INotificationHandler<GuildMemberAddedNotification>,
    INotificationHandler<GuildMemberRemovedNotification>,
    INotificationHandler<BufferedMemberLeftNotification>
{
    private const int MaxUsernamesToDisplay = 100;
    private const string GoodbyeMessageTemplateType = "goodbye";
    private const string FallbackGoodbyeMessageTemplate = "$USER has left. Goodbye!";
    private const int MessageTemplatesCacheDurationMinutes = 5;

    private readonly DiscordLogger _discordLogger;
    private readonly BotSettingsService _botSettingsService;
    private readonly GoodbyeMessageBuffer _goodbyeMessageBuffer;
    private readonly MessageTemplateRepository _messageTemplateRepo;
    private readonly SharedCache _cache;

    public GreetingHandler(
        DiscordLogger discordLogger,
        BotSettingsService botSettingsService,
        GoodbyeMessageBuffer goodbyeMessageBuffer,
        MessageTemplateRepository messageTemplateRepo,
        SharedCache cache)
    {
        _discordLogger = discordLogger;
        _botSettingsService = botSettingsService;
        _goodbyeMessageBuffer = goodbyeMessageBuffer;
        _messageTemplateRepo = messageTemplateRepo;
        _cache = cache;
    }

    public async Task Handle(GuildMemberAddedNotification notification, CancellationToken cancellationToken)
    {
        string memberMention = notification.EventArgs.Member.Mention;

        string? greetingMessage = await _botSettingsService.GetGreetingsMessage(memberMention);

        if (greetingMessage == null)
        {
            throw new Exception("Could not load greeting message");
        }

        _discordLogger.LogGreetingMessage(greetingMessage);
    }

    public Task Handle(GuildMemberRemovedNotification notification, CancellationToken cancellationToken)
    {
        string memberName = notification.EventArgs.Member.DisplayName;

        _goodbyeMessageBuffer.AddUser(memberName);

        return Task.CompletedTask;
    }

    public async Task Handle(BufferedMemberLeftNotification notification, CancellationToken cancellationToken)
    {
        var userList = notification.Usernames.ToList();

        if (userList.Count == 0) return;

        if (userList.Count == 1)
        {
            _discordLogger.LogGreetingMessage(await GetRandomGoodbyeMessage(userList.First()));
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

    private async Task<string> GetRandomGoodbyeMessage(string username)
    {
        string messageTemplate;

        var goodbyeMessages = ((await _cache.LoadFromCacheAsync(
                                SharedCacheKeys.GoodbyeMessages,
                                async () => await _messageTemplateRepo.GetAllMessageTemplates(GoodbyeMessageTemplateType),
                                TimeSpan.FromMinutes(MessageTemplatesCacheDurationMinutes)))
                                    ?? Enumerable.Empty<MessageTemplate>())
                                        .ToList();

        if (goodbyeMessages.Count > 0)
        {
            var idx = new Random().Next(0, goodbyeMessages.Count);
            messageTemplate = goodbyeMessages[idx].Message;
        }
        else
        {
            messageTemplate = FallbackGoodbyeMessageTemplate;
        }

        var formattedGoodbyeMessage = messageTemplate.Replace("$USER", username);

        return formattedGoodbyeMessage;
    }
}
