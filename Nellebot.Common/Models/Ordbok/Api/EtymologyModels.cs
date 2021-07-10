using Nellebot.Common.Models.Ordbok.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok.Api
{
    [JsonConverter(typeof(EtymologyGroupConverter))]
    public abstract class EtymologyGroup : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class EtymologyLanguage : EtymologyGroup
    {
        [JsonPropertyName("items")]
        public List<EtymologyLanguageElement> EtymologyLanguageElements { get; set; } = new List<EtymologyLanguageElement>();
    }

    [JsonConverter(typeof(EtymologyLanguageElementConverter))]
    public abstract class EtymologyLanguageElement : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class EtymologyLanguageLanguage : EtymologyLanguageElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyLanguageRelation : EtymologyLanguageElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyLanguageUsage : EtymologyLanguageElement
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class EtymologyReference : EtymologyGroup
    {
        [JsonPropertyName("items")]
        public List<EtymologyReferenceElement> EtymologyReferenceElements { get; set; } = new List<EtymologyReferenceElement>();
    }

    [JsonConverter(typeof(EtymologyReferenceElementConverter))]
    public abstract class EtymologyReferenceElement : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class EtymologyReferenceRelation : EtymologyReferenceElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyReferenceArticleRef : EtymologyReferenceElement
    {
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
    }
}
