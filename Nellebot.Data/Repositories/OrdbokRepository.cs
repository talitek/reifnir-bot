using System.Threading;
using System.Threading.Tasks;
using Nellebot.Common.Models.Ordbok.Store;

namespace Nellebot.Data.Repositories;

public class OrdbokRepository
{
    private readonly BotDbContext _dbContext;

    public OrdbokRepository(BotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<OrdbokArticleStore?> GetArticleStore(string dictionary, string wordClass, CancellationToken cancellationToken = default)
    {
        return _dbContext.OrdbokArticlesStore.FindAsync(new[] { dictionary, wordClass }, cancellationToken).AsTask();
    }

    public async Task SaveArticleStore(OrdbokArticleStore ordbokArticleStore, CancellationToken cancellationToken = default)
    {
        var existingArticleStore = await _dbContext.OrdbokArticlesStore.FindAsync(
            new[] { ordbokArticleStore.Dictionary, ordbokArticleStore.WordClass },
            cancellationToken);

        if (existingArticleStore == null)
        {
            _dbContext.Add(ordbokArticleStore);
        }
        else
        {
            existingArticleStore.ArticleCount = ordbokArticleStore.ArticleCount;
            existingArticleStore.ArticleList = ordbokArticleStore.ArticleList;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<OrdbokConceptStore?> GetConceptStore(string dictionary, CancellationToken cancellationToken = default)
    {
        return _dbContext.OrdbokConceptStore.FindAsync(new[] { dictionary }, cancellationToken).AsTask();
    }

    public async Task SaveConceptStore(OrdbokConceptStore ordbokConcepts, CancellationToken cancellationToken = default)
    {
        var existingConceptStore = await _dbContext.OrdbokConceptStore.FindAsync(
            new[] { ordbokConcepts.Dictionary },
            cancellationToken);

        if (existingConceptStore == null)
        {
            _dbContext.Add(ordbokConcepts);
        }
        else
        {
            existingConceptStore.Concepts = ordbokConcepts.Concepts;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
