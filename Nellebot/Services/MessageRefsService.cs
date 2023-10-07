using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Logging;
using Nellebot.Data.Repositories;
using Nellebot.Services.Loggers;
using Nellebot.Utils;

namespace Nellebot.Services;

public class MessageRefsService
{
    private readonly DiscordResolver _discordResolver;
    private readonly IDiscordErrorLogger _discordErrorLogger;
    private readonly ILogger<MessageRefsService> _logger;
    private readonly MessageRefRepository _messageRefRepo;

    public MessageRefsService(DiscordResolver discordResolver, IDiscordErrorLogger discordErrorLogger, ILogger<MessageRefsService> logger, MessageRefRepository messageRefRepo)
    {
        _discordResolver = discordResolver;
        _discordErrorLogger = discordErrorLogger;
        _logger = logger;
        _messageRefRepo = messageRefRepo;
    }

    public async Task<int> PopulateMessageRefs(DateTimeOffset lastHeartbeat, string eventSource)
    {
        var guild = _discordResolver.ResolveGuild();

        var channels = guild.Channels.Values.Where(c => c.Type == DSharpPlus.ChannelType.Text).ToList();

        if (channels == null || !channels.Any())
        {
            _discordErrorLogger.LogWarning("No channels found", "No channels found when populating message refs");
            _logger.LogWarning("No channels found");
            return 0;
        }

        int messageRefCreatedCount = 0;

        var lastHeartbeatWithMargin = lastHeartbeat - TimeSpan.FromMinutes(5);

        ulong lastHeartbeatSnowflake = GetSnowflakeFromDateTimeOffset(lastHeartbeatWithMargin);

        const int messageBatchSize = 100;

        foreach (var channel in channels)
        {
            try
            {
                if (channel is null)
                {
                    _discordErrorLogger.LogWarning("PopulateMessageRefs", "Channel was null");
                    continue;
                }

                try
                {
                    await foreach (var message in channel.GetMessagesAfterAsync(lastHeartbeatSnowflake, messageBatchSize))
                    {
                        if (message is null)
                        {
                            _discordErrorLogger.LogWarning($"PopulateMessageRefsInit ({eventSource})", "Message was null");
                            continue;
                        }

                        if (message.Author.IsCurrent)
                        {
                            continue;
                        }

                        bool created = await _messageRefRepo.CreateMessageRefIfNotExists(message.Id, message.Channel.Id, message.Author.Id);

                        if (created)
                        {
                            messageRefCreatedCount++;
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    _logger.LogError(ex, "NullReferenceException in PopulateMessageRefs. LastHeartbeatSnowflake: {LastHeartbeatSnowflake}. EventSource: {EventSource}", lastHeartbeatSnowflake, eventSource);
                    _discordErrorLogger.LogWarning("PopulateMessageRefs", $"NullReferenceException in PopulateMessageRefs. LastHeartbeatSnowflake: {lastHeartbeatSnowflake}. EventSource: {eventSource}");
                    continue;
                }
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedException)
                {
                    continue;
                }

                string errorMessage = $"{nameof(PopulateMessageRefs)}: {channel.Name}";

                _discordErrorLogger.LogError(ex, errorMessage);
                _logger.LogError(ex, errorMessage);
            }
        }

        return messageRefCreatedCount;
    }

    public async Task<IList<DiscordMessage>> PopulateMessageRefsInit(DiscordGuild guild)
    {
        var createdMessages = new List<DiscordMessage>();

        var channels = guild.Channels.Values.Where(c => c.Type == DSharpPlus.ChannelType.Text).ToList();

        const int messageBatchSize = 1000;

        foreach (var channel in channels)
        {
            try
            {
                var lastMessageSnowflake = channel.LastMessageId;

                if (lastMessageSnowflake == null)
                {
                    continue;
                }

                try
                {
                    await foreach (var message in channel.GetMessagesBeforeAsync((ulong)lastMessageSnowflake, messageBatchSize))
                    {
                        if (message is null)
                        {
                            _discordErrorLogger.LogWarning("PopulateMessageRefsInit", "Message was null");
                            continue;
                        }

                        if (message.Author.IsCurrent)
                        {
                            continue;
                        }

                        bool created = await _messageRefRepo.CreateMessageRefIfNotExists(message.Id, message.Channel.Id, message.Author.Id);

                        if (created)
                        {
                            createdMessages.Add(message);
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    _logger.LogError(ex, "NullReferenceException in PopulateMessageRefs. LastMessageSnowflake: {LastMessageSnowflake}.", lastMessageSnowflake);
                    _discordErrorLogger.LogWarning("PopulateMessageRefs", $"NullReferenceException in PopulateMessageRefs. LastMessageSnowflake: {lastMessageSnowflake}.");
                    continue;
                }
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedException)
                {
                    continue;
                }

                string errorMessage = $"{nameof(PopulateMessageRefsInit)}: {channel.Name}";

                _discordErrorLogger.LogError(ex, errorMessage);

                _logger.LogError(ex, "{errorMessage}", errorMessage);
            }
        }

        return createdMessages;
    }

    private static ulong GetSnowflakeFromDateTimeOffset(DateTimeOffset dateTime)
    {
        long discordEpochMs = DiscordConstants.DiscordEpochMs;
        long dateTimeMs = dateTime.ToUnixTimeMilliseconds();

        long snowflake = (dateTimeMs - discordEpochMs) << 22;

        return (ulong)snowflake;
    }
}
