using Nellebot.Common.Models.Glosbe.Dom;
using PuppeteerSharp;
using System.Linq;
using System.Threading.Tasks;

namespace Nellebot.Services.Glosbe
{
    public class GlosbeClient
    {
        private readonly PuppeteerFactory _puppeteerFactory;

        public GlosbeClient(PuppeteerFactory puppeteerFactory)
        {
            _puppeteerFactory = puppeteerFactory;
        }

        public async Task<GlosbeTranslationResult> GetTranslation(string originalLanguage, string targetLanguage, string query)
        {
            var translationResult = new GlosbeTranslationResult();

            var article = new Article();

            var queryUrl = $"https://glosbe.com/{originalLanguage}/{targetLanguage}/{query}";

            using (var browser = await _puppeteerFactory.BuildBrowser())
            using (var page = await browser.NewPageAsync())
            {
                var response = await page.GoToAsync(queryUrl);

                // Expand definitions
                var expandDefinitionsButtonQueryAll = "button[data-element='definitions-more']";
                var expandDefinitionsButtonElements = await page.QuerySelectorAllAsync(expandDefinitionsButtonQueryAll);
                if (expandDefinitionsButtonElements != null)
                {
                    foreach (var item in expandDefinitionsButtonElements)
                    {
                        await item.ClickAsync();
                    }
                    await page.WaitForSelectorAsync(expandDefinitionsButtonQueryAll, new WaitForSelectorOptions() { Hidden = true });
                    await Task.Delay(100);// Wait for the divs to expand. TODO find a better solution
                }

                var firstPhraseTranslationQuery = "div.phrase__translation";
                var firstPhraseTranslationElement = await page.QuerySelectorAsync(firstPhraseTranslationQuery);

                var originalPhraseQuery = "div.phrase__translation__summary h2[data-element='phrase']";
                var originalPhraseElement = await firstPhraseTranslationElement.QuerySelectorAsync(originalPhraseQuery);
                var originalPhraseTextValue = await GetElementTextValueAsync(originalPhraseElement);

                article.Phrase = originalPhraseTextValue;

                var originalSummaryFieldQueryAll = "div.phrase__translation__summary span.phrase__summary__field";
                var originalSummaryFieldElements = await firstPhraseTranslationElement.QuerySelectorAllAsync(originalSummaryFieldQueryAll);
                var originalSummaryFieldTextValues = await GetElementTextValuesAsync(originalSummaryFieldElements);

                article.SummaryFields = originalSummaryFieldTextValues;

                var translationItemsQueryAll = "ul.translations__list > li.translation__item";
                var translationItemElements = await firstPhraseTranslationElement.QuerySelectorAllAsync(translationItemsQueryAll);

                foreach (var translationItemElement in translationItemElements)
                {
                    var translationItem = new TranslationItem();

                    var translationPhraseQuery = "span.translation__item__phrase > h3.translation > span";
                    var translationPhraseElement = await translationItemElement.QuerySelectorAsync(translationPhraseQuery);
                    var translationPhraseTextValue = await GetElementTextValueAsync(translationPhraseElement);

                    translationItem.Phrase = translationPhraseTextValue;

                    var translationSummaryQuery = "span.phrase__summary__field";
                    var translationSummaryElement = await translationItemElement.QuerySelectorAsync(translationSummaryQuery);
                    var translationSummaryTextValue = await GetElementTextValueAsync(translationSummaryElement);

                    translationItem.SummaryField = translationSummaryTextValue;

                    var translationDefinitionQueryAll = "p.translation__definition";
                    var translationDefinitionElements = await translationItemElement.QuerySelectorAllAsync(translationDefinitionQueryAll);

                    foreach (var translationDefinitionElement in translationDefinitionElements)
                    {
                        var translationDefinition = new TranslationDefinition();

                        var translationDefinitionSpanQueryAll = "span";
                        var translationDefinitionSpanElements = await translationDefinitionElement.QuerySelectorAllAsync(translationDefinitionSpanQueryAll);
                        var translationDefinitionSpanTextValues = await GetElementTextValuesAsync(translationDefinitionSpanElements);

                        translationDefinition.Values = translationDefinitionSpanTextValues;

                        translationItem.TranslationDefinitions.Add(translationDefinition);
                    }

                    var translationExampleOriginalQuery = "div.translation__example > p:nth-child(1)";
                    var translationExampleOriginalElement = await translationItemElement.QuerySelectorAsync(translationExampleOriginalQuery);
                    var translationExampleOriginalTextValue = await GetElementTextValueAsync(translationExampleOriginalElement);

                    var translationExampleTargetQuery = "div.translation__example > p:nth-child(2)";
                    var translationExampleTargetElement = await translationItemElement.QuerySelectorAsync(translationExampleTargetQuery);
                    var translationExampleTargetTextValue = await GetElementTextValueAsync(translationExampleTargetElement);

                    if (!string.IsNullOrWhiteSpace(translationExampleOriginalTextValue)
                        && !string.IsNullOrWhiteSpace(translationExampleOriginalTextValue))
                    {
                        translationItem.TranslationExample = new TranslationExample()
                        {
                            OriginalLanguageExample = translationExampleOriginalTextValue,
                            TargetLanguageExample = translationExampleTargetTextValue
                        };
                    }


                    article.TranslationItems.Add(translationItem);
                }
            }

            translationResult.Article = article;
            translationResult.QueryUrl = queryUrl;
            translationResult.Query = query;
            translationResult.OriginalLanguage = originalLanguage;
            translationResult.TargetLanguage = targetLanguage;

            return translationResult;
        }

        private static async Task<string> GetElementTextValueAsync(ElementHandle handle)
        {
            if (handle == null)
                return string.Empty;

            var innerTextPro = await handle.GetPropertyAsync("innerText");
            var innerTextValue = await innerTextPro.JsonValueAsync<string>();

            return innerTextValue?.Trim() ?? string.Empty;
        }

        private static async Task<string[]> GetElementTextValuesAsync(ElementHandle[] handles)
        {
            return await Task.WhenAll(handles.Select(GetElementTextValueAsync));
        }
    }
}
