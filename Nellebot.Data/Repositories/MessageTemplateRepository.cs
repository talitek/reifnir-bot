using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nellebot.Common.Models;
using Nellebot.Common.Utils;

namespace Nellebot.Data.Repositories;

public class MessageTemplateRepository
{
    private readonly BotDbContext _dbContext;

    public MessageTemplateRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateMessageTemplate(string message, string type, ulong authorId, CancellationToken cancellationToken = default)
    {
        var messageTemplate = new MessageTemplate
        {
            Id = PseudonymGenerator.NewFriendlyId(),
            Message = message,
            Type = type,
            AuthorId = authorId,
            DateTime = DateTime.UtcNow,
        };

        _dbContext.Add(messageTemplate);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MessageTemplate?> GetMessageTemplate(string id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MessageTemplates.FindAsync(new[] { id }, cancellationToken);
    }

    public async Task<IEnumerable<MessageTemplate>> GetAllMessageTemplates(string type, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MessageTemplates
            .Where(x => x.Type == type)
            .OrderByDescending(x => x.DateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteMessageTemplate(string id, CancellationToken cancellationToken = default)
    {
        var messageTemplate = await _dbContext.MessageTemplates.FindAsync(new[] { id }, cancellationToken);

        if (messageTemplate == null) return;

        _dbContext.Remove(messageTemplate);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
