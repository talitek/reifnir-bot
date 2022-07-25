using DSharpPlus.Entities;
using Nellebot.Data.Repositories;
using Nellebot.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nellebot.Services;

public class MessageRefsService
{
    private readonly DiscordResolver _discordResolver;
    private readonly MessageRefRepository _messageRefRepo;

    public MessageRefsService(DiscordResolver discordResolver, MessageRefRepository messageRefRepo)
    {
        _discordResolver = discordResolver;
        _messageRefRepo = messageRefRepo;
    }

    public async Task<int> PopulateMessageRefs(DateTimeOffset lastHeartbeat)
    {
        var guild = _discordResolver.ResolveGuild();

        var channels = guild.Channels.Values.Where(c => c.Type == DSharpPlus.ChannelType.Text);

        var messageRefCreatedCount = 0;

        var lastHeartbeatWithMargin = lastHeartbeat - TimeSpan.FromMinutes(5);

        var lastHeartbeatSnowflake = GetSnowflakeFromDateTimeOffset(lastHeartbeatWithMargin);

        const int messageBatchSize = 100;

        foreach (var channel in channels)
        {
            try
            {
                var messages = (await channel.GetMessagesAfterAsync(lastHeartbeatSnowflake, messageBatchSize))
                                .Where(m => !m.Author.IsCurrent)
                                .ToList();

                foreach (var message in messages)
                {
                    var created = await _messageRefRepo.CreateMessageRefIfNotExists(message.Id, message.Channel.Id, message.Author.Id);

                    if (created) messageRefCreatedCount++;
                }
            }
            catch (Exception ex)
            {
                if(ex.Message == "Unauthorized: 403")
                    continue;

                throw;
            }
        }

        return messageRefCreatedCount;
    }

    public async Task<int> PopulateMessageRefsInit(DiscordGuild guild, DiscordChannel outputChannel)
    {
        var channels = guild.Channels.Values;

        var messageRefCountTotal = 0;
        
        const int messageBatchSize = 100;
        const int messageBatches = 10;

        foreach (var channel in channels)
        {
            try
            {
                var messageRefCountChannel = 0;

                var lastMessageSnowflake = channel.LastMessageId;

                if (lastMessageSnowflake == null) continue;

                for (int i = 0; i < messageBatches; i++)
                {
                    var messages = (await channel.GetMessagesBeforeAsync((ulong)lastMessageSnowflake, messageBatchSize))
                                    .Where(m => !m.Author.IsCurrent)
                                    .ToList();

                    if (messages.Count == 0) break;

                    foreach (var message in messages)
                    {
                        var created = await _messageRefRepo.CreateMessageRefIfNotExists(message.Id, message.Channel.Id, message.Author.Id);

                        if (created) messageRefCountChannel++;
                    }

                    lastMessageSnowflake = messages.Min(m => m.Id);
                }

                if (messageRefCountChannel > 0)
                    await outputChannel.SendMessageAsync($"Populated {messageRefCountChannel} message refs in {channel.Name}");

                messageRefCountTotal += messageRefCountChannel;
            }
            catch (Exception)
            {
                continue;
            }
        }

        return messageRefCountTotal;
    }

    private static ulong GetSnowflakeFromDateTimeOffset(DateTimeOffset dateTime)
    {
        var discordEpochMs = DiscordConstants.DiscordEpochMs;
        var dateTimeMs = (dateTime).ToUnixTimeMilliseconds();

        var snowflake = dateTimeMs - discordEpochMs << 22;

        return (ulong)snowflake;
    }
}
