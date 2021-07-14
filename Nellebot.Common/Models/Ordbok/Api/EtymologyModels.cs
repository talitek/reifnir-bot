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

    // EtymologyLanguage

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

    /// <summary>
    /// Api types so far: relation, language, grammar
    /// </summary>
    public class EtymologyLanguageIdElement : EtymologyLanguageElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// Api types so far: usage
    /// </summary>
    public class EtymologyLanguageTextElement : EtymologyLanguageElement
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    // EtymologyReference

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

    /// Api types so far: relation, entity, grammar
    public class EtymologyReferenceIdElement : EtymologyReferenceElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class EtymologyReferenceArticleRef : EtymologyReferenceElement
    {
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
        [JsonPropertyName("definition_id")]
        public int? DefinitionId { get; set; }
        [JsonPropertyName("lemmas")]
        public List<SimpleLemma> Lemmas { get; set; } = new List<SimpleLemma>();
    }

    // Etymology litt?

    public class EtymologyLitt: EtymologyGroup
    {
        [JsonPropertyName("items")]
        public List<EtymologyLittElement> EtymologyLittElements { get; set; } = new List<EtymologyLittElement>();
    }

    [JsonConverter(typeof(EtymologyLittElementConverter))]
    public abstract class EtymologyLittElement: ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Api types so far: entity
    /// </summary>
    public class EtymologyLittIdElement : EtymologyLittElement
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// Api types so far: usage
    /// </summary>
    public class EtymologyLittTextElement : EtymologyLittElement
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
