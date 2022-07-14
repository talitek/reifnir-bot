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

        public async Task<AwardMessage?> GetAwardMessageByOriginalMessageId(ulong awardChannelId, ulong originalMessageId)
        {
            var awardMessage = await _dbContext.AwardMessages
                                .SingleOrDefaultAsync(m => m.OriginalMessageId == originalMessageId
                                                        && m.AwardChannelId == awardChannelId);

            return awardMessage;
        }

        public async Task<AwardMessage?> GetAwardMessageByAwardedMessageId(ulong awardChannelId, ulong awardedMessageId)
        {
            var awardMessage = await _dbContext.AwardMessages
                                .SingleOrDefaultAsync(m => m.AwardedMessageId == awardedMessageId
                                                        && m.AwardChannelId == awardChannelId);

            return awardMessage;
        }

        public async Task<AwardMessage> CreateAwardMessage(
            ulong originalMessageId,
            ulong originalChannelId,
            ulong awardedMessageId,
            ulong awardChannelId,
            ulong userId,
            uint awardCount)
        {
            var awardMessage = new AwardMessage
            {
                OriginalMessageId = originalMessageId,
                OriginalChannelId = originalChannelId,
                UserId = userId,
                AwardedMessageId = awardedMessageId,
                AwardChannelId = awardChannelId,
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

            if (existingMessage != null)
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

        public async Task<UserAwardStats> GetAwardStatsForUser(ulong userId)
        {
            var awardStats = new UserAwardStats();

            awardStats.TotalAwardCount = (uint)await _dbContext.AwardMessages
                .Where(m => m.UserId == userId)
                .SumAsync(a => a.AwardCount);

            awardStats.AwardMessageCount = (uint)await _dbContext.AwardMessages
                .Where(m => m.UserId == userId)
                .CountAsync();

            awardStats.TopAwardedMessages = await _dbContext.AwardMessages
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.AwardCount)
                .Take(10)
                .ToListAsync();

            return awardStats;
        }
    }
}
