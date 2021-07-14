using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api
{
    public class Article
    {
        [JsonPropertyName("score")]
        public double Score { get; set; }
        [JsonPropertyName("dictionary")]
        public string Dictionary { get; set; } = string.Empty;
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
        [JsonPropertyName("body")]
        public Body Body { get; set; } = null!;
        [JsonPropertyName("lemmas")]
        public List<Lemma> Lemmas { get; set; } = new List<Lemma>();
    }

    public class Body
    {
        [JsonPropertyName("definitions")]
        public List<DefinitionElement> DefinitionElements { get; set; } = new List<DefinitionElement>();
        [JsonPropertyName("etymology")]
        public List<EtymologyGroup> EtymologyGroups { get; set; } = new List<EtymologyGroup>();
    }

    public class SubArticle
    {
        [JsonPropertyName("body")]
        public SubArticleBody Body { get; set; } = null!;
    }

    public class SubArticleBody
    {
        [JsonPropertyName("definitions")]
        public List<DefinitionElement> DefinitionElements { get; set; } = new List<DefinitionElement>();
        [JsonPropertyName("lemmas")]
        public List<Lemma> Lemmas { get; set; } = new List<Lemma>();
    }
}
