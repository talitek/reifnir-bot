using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vm = Nellebot.Common.Models.Glosbe.ViewModels;
using dom = Nellebot.Common.Models.Glosbe.Dom;

namespace Nellebot.Services.Glosbe
{
    public class GlosbeModelMapper
    {
        public vm.GlosbeSearchResult MapTranslationResult(dom.GlosbeTranslationResult result)
        {
            var vmResult = new vm.GlosbeSearchResult();

            var query = result.Query;
            var originalLanguage = result.OriginalLanguage;
            var targetLanaguage = result.TargetLanguage;

            vmResult.Article = MapArticle(result.Article, query, originalLanguage, targetLanaguage);
            vmResult.QueryUrl = result.QueryUrl;

            return vmResult;
        }

        private vm.Article MapArticle(dom.Article article, string query, string originalLanguage, string targetLanguage)
        {
            var vmResult = new vm.Article();

            vmResult.Lemma = article.Phrase;
            vmResult.GrammarItems = article.SummaryFields.Select(x => x).ToList();
            vmResult.TranslationItems = article.TranslationItems.Select(x => MapTranslationArticle(x, query, originalLanguage, targetLanguage)).ToList();

            return vmResult;
        }

        private vm.TranslationItem MapTranslationArticle(dom.TranslationItem translationItem, string query, string originalLanguage, string targetLanguage)
        {
            var vmResult = new vm.TranslationItem();

            vmResult.Lemma = translationItem.Phrase;
            vmResult.Grammar = translationItem.SummaryField;
            vmResult.TranslationDefinitionGroups = translationItem.TranslationDefinitions.Select(MapTranslationDefinitionGroup).ToList();

            if(translationItem.TranslationExample != null)
            {
                vmResult.TranslationExample = new vm.TranslationExample
                {
                    OriginalLanguageValue = originalLanguage,
                    OriginalLanguageExample = MarkKeywordInText(query, translationItem.TranslationExample.OriginalLanguageExample),
                    TargetLanguageValue = targetLanguage,
                    TargetLanguageExample = MarkKeywordInText(translationItem.Phrase, translationItem.TranslationExample.TargetLanguageExample),
                };
            }           

            return vmResult;
        }

        private vm.TranslationDefinitionGroup MapTranslationDefinitionGroup(dom.TranslationDefinition translationDefinition)
        {
            var vmResult = new vm.TranslationDefinitionGroup();

            vmResult.TranslationDefinitions = MapTranslationDefinitions(translationDefinition.Values);

            return vmResult;
        }

        private List<vm.TranslationDefinition> MapTranslationDefinitions(string[] values)
        {
            var vmResult = new List<vm.TranslationDefinition>();

            for (int i = 0; i < values.Length; i += 2)
            {
                var translationDefinition = new vm.TranslationDefinition();

                translationDefinition.Language = values[i];
                translationDefinition.Value = values[i + 1];

                vmResult.Add(translationDefinition);
            }

            return vmResult;
        }

        private string MarkKeywordInText(string keyword, string text)
        {
            return text.Replace(keyword, $"**{keyword}**");
        }
    }
}
