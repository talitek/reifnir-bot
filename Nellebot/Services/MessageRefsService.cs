using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
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

    public async Task<int> PopulateMessageRefs(DateTimeOffset lastHeartbeat)
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

                IReadOnlyList<DiscordMessage>? messagesAfter = null;

                try
                {
                    messagesAfter = await channel.GetMessagesAfterAsync(lastHeartbeatSnowflake, messageBatchSize);
                }
                catch (NullReferenceException)
                {
                    _discordErrorLogger.LogWarning("PopulateMessageRefs", "channel.GetMessagesAfterAsync() threw a null reffy");
                    continue;
                }

                if (messagesAfter == null)
                {
                    _discordErrorLogger.LogWarning("PopulateMessageRefs", "GetMessagesAfterAsync returned null");
                    continue;
                }

                var messageByAuthor = messagesAfter.Where(m => m.Author != null && !m.Author.IsCurrent).ToList();

                foreach (var message in messageByAuthor)
                {
                    bool created = await _messageRefRepo.CreateMessageRefIfNotExists(message.Id, message.Channel.Id, message.Author.Id);

                    if (created)
                    {
                        messageRefCreatedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Trim() == DiscordConstants.UnauthorizedErrorMessage)
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

        IEnumerable<DiscordChannel> channels = guild.Channels.Values;

        const int messageBatchSize = 100;
        const int messageBatches = 10;

        foreach (DiscordChannel? channel in channels)
        {
            try
            {
                ulong? lastMessageSnowflake = channel.LastMessageId;

                if (lastMessageSnowflake == null)
                {
                    continue;
                }

                for (int i = 0; i < messageBatches; i++)
                {
                    var messages = (await channel.GetMessagesBeforeAsync((ulong)lastMessageSnowflake, messageBatchSize))
                                    .Where(m => !m.Author.IsCurrent)
                                    .ToList();

                    if (messages.Count == 0)
                    {
                        break;
                    }

                    foreach (DiscordMessage? message in messages)
                    {
                        bool created = await _messageRefRepo.CreateMessageRefIfNotExists(message.Id, message.Channel.Id, message.Author.Id);

                        if (created)
                        {
                            createdMessages.Add(message);
                        }
                    }

                    lastMessageSnowflake = messages.Min(m => m.Id);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == DiscordConstants.UnauthorizedErrorMessage)
                {
                    continue;
                }

                string errorMessage = $"{nameof(PopulateMessageRefsInit)}: {channel.Name}";

                _discordErrorLogger.LogError(ex, errorMessage);

                _logger.LogError(ex, errorMessage);
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
