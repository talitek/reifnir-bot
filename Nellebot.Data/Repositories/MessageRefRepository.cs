using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using System;
using System.Threading.Tasks;

namespace Nellebot.Data.Repositories
{
    public class MessageRefRepository
    {
        private readonly BotDbContext _dbContext;

        public MessageRefRepository(BotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task CreateMessageRef(ulong messageId, ulong channelId, ulong userId)
        {
            try
            {
                var messageRef = new MessageRef
                {
                    MessageId = messageId,
                    ChannelId = channelId,
                    UserId = userId,
                    DateTime = DateTime.UtcNow
                };

                await _dbContext.AddAsync(messageRef);
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ex.InnerException is Npgsql.PostgresException pgEx)
                {
                    if (pgEx.SqlState != Npgsql.PostgresErrorCodes.UniqueViolation)
                        throw;
                }
                else
                {
                    throw;
                }
            }
        }

        public Task<MessageRef?> GetMessageRef(ulong messageId)
        {
            return _dbContext.MessageRefs.FindAsync(messageId).AsTask();
        }

        public async Task<bool> CreateMessageRefIfNotExists(ulong messageId, ulong channelId, ulong userId)
        {
            var messageRef = await GetMessageRef(messageId);

            if (messageRef != null) return false;

            await CreateMessageRef(messageId, channelId, userId);

            return true;
        }
    }
}
