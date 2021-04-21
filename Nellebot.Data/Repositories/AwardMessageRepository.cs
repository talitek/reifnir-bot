using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Data.Repositories
{
    public class AwardMessageRepository
    {
        private readonly BotDbContext _dbContext;

        public AwardMessageRepository(BotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AwardMessage> GetAwardMessageByOriginalMessageId(ulong originalMessageId)
        {
            var awardMessage = await _dbContext.AwardMessages
                                .SingleOrDefaultAsync(m => m.OriginalMessageId == originalMessageId);

            return awardMessage;
        }

        public async Task<AwardMessage> CreateAwardMessage(ulong originalMessageId, ulong awardedMessageId, ulong userId, uint awardCount)
        {
            var awardMessage = new AwardMessage
            {
                OriginalMessageId = originalMessageId,
                UserId = userId,
                AwardedMessageId = awardedMessageId,
                AwardCount = awardCount,
                DateTime = DateTime.UtcNow
            };

            await _dbContext.AddAsync(awardMessage);

            await _dbContext.SaveChangesAsync();

            return awardMessage;
        }

        public async Task DeleteAwardMessage(Guid id)
        {
            var existingMessage = await _dbContext.AwardMessages.FindAsync(id);

            if(existingMessage != null)
            {
                _dbContext.Remove(existingMessage);

                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task UpdateAwardCount(Guid id, uint awardCount)
        {
            var existingMessage = await _dbContext.AwardMessages.FindAsync(id);

            if (existingMessage != null)
            {
                existingMessage.AwardCount = awardCount;

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
