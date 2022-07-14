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

        public Task<MessageRef?> GetMessageRef(ulong messageId)
        {
            return _dbContext.MessageRefs.FindAsync(messageId).AsTask();
        }
    }
}
