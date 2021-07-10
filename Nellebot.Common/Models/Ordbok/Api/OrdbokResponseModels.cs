using Nellebot.Common.Models.Ordbok.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok.Api
{
    public class OrdbokSearchResponse : List<Article> { }

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
}
