using Nellebot.Common.Models.Ordbok.Converters;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api
{
    [JsonConverter(typeof(DefinitionElementConverter))]
    public abstract class DefinitionElement : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class Definition : DefinitionElement
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("elements")]
        public List<DefinitionElement> DefinitionElements { get; set; } = new List<DefinitionElement>();
    }

    public class Explanation : DefinitionElement
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        [JsonPropertyName("items")]
        public List<ExplanationItem> ExplanationItems { get; set; } = new List<ExplanationItem>();
    }

    [JsonConverter(typeof(ExplanationItemConverter))]
    public abstract class ExplanationItem : ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    /// Api types so far: relation, domain, entity, modifier, grammar, rhetoric, language
    /// </summary>
    public class ExplanationIdItem: ExplanationItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    /// <summary>
    /// Api types so far: usage
    /// </summary>
    public class ExplanationTextItem : ExplanationItem
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class ExplanationArticleRefItem : ExplanationItem
    {
        [JsonPropertyName("article_id")]
        public int ArticleId { get; set; }
        [JsonPropertyName("definition_order")]
        public int DefinitionOrder { get; set; }
        [JsonPropertyName("lemmas")]
        public List<SimpleLemma> Lemmas { get; set; } = new List<SimpleLemma>();
    }

    public class Example : DefinitionElement
    {
        [JsonPropertyName("quote")]
        public Quote Quote { get; set; } = null!;
        [JsonPropertyName("explanation")]
        public ExampleExplanation Explanation { get; set; } = null!;
    }

    public class Quote
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        [JsonPropertyName("items")]
        public List<QuoteItem> QuoteItems { get; set; } = new List<QuoteItem>();
    }

    [JsonConverter(typeof(QuoteItemConverter))]
    public abstract class QuoteItem: ITypeElement
    {
        [JsonPropertyName("type_")]
        public string Type { get; set; } = string.Empty;
    }

    public class QuoteIdItem : QuoteItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class QuoteTextItem: QuoteItem
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class ExampleExplanation
    {
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    //public class DefinitionSubArticle : DefinitionElement
    //{
    //    [JsonPropertyName("article_id")]
    //    public int ArticleId { get; set; }
    //    [JsonPropertyName("article")]
    //    public SubArticle Article { get; set; } = null!;
    //    // intro?
    //}
}
