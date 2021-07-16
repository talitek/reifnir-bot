using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Glosbe.ViewModels
{
    public class GlosbeSearchResult
    {
        public Article Article { get; set; } = new Article();
        public string QueryUrl { get; set; } = string.Empty;
    }

    public class Article
    {
        public string Lemma { get; set; } = string.Empty;
        // e.g. Masculine noun, verb, etc. 
        public List<string> GrammarItems { get; set; } = new List<string>();
        public List<TranslationItem> TranslationItems { get; set; } = new List<TranslationItem>();
    }

    public class TranslationItem
    {
        public string Lemma { get; set; } = string.Empty;
        // e.g. Masculine noun, verb, etc. 
        public string Grammar { get; set; } = string.Empty;
        public List<TranslationDefinitionGroup> TranslationDefinitionGroups { get; set; } = new List<TranslationDefinitionGroup>();
        public TranslationExample? TranslationExample { get; set; }
    }

    public class TranslationDefinitionGroup
    {
        public List<TranslationDefinition> TranslationDefinitions { get; set; } = new List<TranslationDefinition>();
    }

    public class TranslationDefinition
    {
        public string Language { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class TranslationExample
    {
        public string OriginalLanguageValue { get; set; } = string.Empty;
        public string OriginalLanguageExample { get; set; } = string.Empty;
        public string TargetLanguageValue { get; set; } = string.Empty;
        public string TargetLanguageExample { get; set; } = string.Empty;        
    }
}
