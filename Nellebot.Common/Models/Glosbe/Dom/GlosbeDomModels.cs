using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Glosbe.Dom
{
    public class GlosbeTranslationResult
    {
        public Article Article { get; set; } = new Article();
        public string QueryUrl { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;        
        public string OriginalLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        
    }

    public class Article
    {
        public string Phrase { get; set; } = string.Empty;
        // e.g. Masculine noun, verb, etc. 
        public string[] SummaryFields { get; set; } = new string[0];
        public List<TranslationItem> TranslationItems { get; set; } = new List<TranslationItem>();
    }

    public class TranslationItem
    {
        public string Phrase { get; set; } = string.Empty;
        public string SummaryField { get; set; } = string.Empty;
        public List<TranslationDefinition> TranslationDefinitions { get; set; } = new List<TranslationDefinition>();
        public TranslationExample? TranslationExample { get; set; }
    }

    public class TranslationDefinition
    {
        public string[] Values { get; set; } = Array.Empty<string>();
    }

    public class TranslationExample
    {
        public string OriginalLanguageExample { get; set; } = string.Empty;
        public string TargetLanguageExample { get; set; } = string.Empty;
    }
}
