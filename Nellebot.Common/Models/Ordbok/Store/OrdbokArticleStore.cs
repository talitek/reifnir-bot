using System.Collections.Generic;

namespace Nellebot.Common.Models.Ordbok.Store;

public record OrdbokArticleStore
{
    public string Dictionary { get; init; } = null!;

    public string WordClass { get; init; } = null!;

    public int ArticleCount { get; set; }

    public IEnumerable<int> ArticleList { get; set; } = new List<int>();
}
