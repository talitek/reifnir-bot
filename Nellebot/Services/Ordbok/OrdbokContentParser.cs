
using System.Collections.Generic;
using System.Linq;
using api = Nellebot.Common.Models.Ordbok.Api;
using System.Text.RegularExpressions;
using Nellebot.Common.Extensions;

namespace Nellebot.Services.Ordbok
{
    /// <summary>
    /// Replaces content which contains $-tokens with values from items array
    /// </summary>
    public interface IOrdbokContentParser
    {
        string GetEtymologyLanguageContent(api.EtymologyLanguage etymologyLanguage, string dictionary);
        string GetEtymologyLittContent(api.EtymologyLitt etymologyLitt, string dictionary);
        string GetEtymologyReferenceContent(api.EtymologyReference reference, string dictionary);
        string GetExplanationContent(api.Explanation explanation, string dictionary);
        string GetExampleContent(api.Example example, string dictionary);
    }

    public class OrdbokContentParser : IOrdbokContentParser
    {
        private readonly ILocalizationService _localizationService;

        public OrdbokContentParser(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public string GetEtymologyLanguageContent(api.EtymologyLanguage etymologyLanguage, string dictionary)
        {
            var contentString = etymologyLanguage.Content;

            var replacementValues = new List<string>();

            foreach (var item in etymologyLanguage.EtymologyLanguageElements)
            {
                switch (item)
                {
                    case api.EtymologyLanguageIdElement idElement:
                        var localizedIdElement = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);
                        replacementValues.Add(localizedIdElement);
                        break;
                    case api.EtymologyLanguageTextElement textElement:
                        replacementValues.Add(textElement.Text);
                        break;
                }
            }

            var finalContentString = ReplaceContentVariables(contentString, replacementValues);

            return finalContentString;
        }

        public string GetEtymologyLittContent(api.EtymologyLitt etymologyLitt, string dictionary)
        {
            var contentString = etymologyLitt.Content;

            var replacementValues = new List<string>();

            foreach (var item in etymologyLitt.EtymologyLittElements)
            {
                switch (item)
                {
                    case api.EtymologyLittIdElement idElement:
                        var localizedIdElement = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);
                        replacementValues.Add(localizedIdElement);
                        break;
                    case api.EtymologyLittTextElement textElement:
                        replacementValues.Add(textElement.Text);
                        break;
                }
            }

            var finalContentString = ReplaceContentVariables(contentString, replacementValues);

            return finalContentString;
        }

        public string GetEtymologyReferenceContent(api.EtymologyReference reference, string dictionary)
        {
            var contentString = reference.Content;

            var replacementValues = new List<string>();

            foreach (var item in reference.EtymologyReferenceElements)
            {
                switch (item)
                {
                    case api.EtymologyReferenceIdElement idElement:
                        var localizedElementId = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);

                        replacementValues.Add(localizedElementId);
                        break;
                    case api.EtymologyReferenceArticleRef articleRef:
                        var firstLemma = articleRef.Lemmas.FirstOrDefault();
                        if (firstLemma != null)
                        {
                            var value = firstLemma.Value;
                            var hgNo = firstLemma.HgNo.ToRomanNumeral();

                            var showHgNo = !string.IsNullOrWhiteSpace(hgNo);

                            var displayValue = showHgNo ? $"{value} ({hgNo})" : value;

                            replacementValues.Add(displayValue);
                        }

                        break;
                }
            }

            var finalContentString = ReplaceContentVariables(contentString, replacementValues);

            return finalContentString;
        }

        public string GetExplanationContent(api.Explanation explanation, string dictionary)
        {
            var contentString = explanation.Content;

            var replacementValues = new List<string>();

            foreach (var item in explanation.ExplanationItems)
            {
                switch (item)
                {
                    case api.ExplanationIdItem idElement:
                        var localizedElementId = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);
                        replacementValues.Add(localizedElementId);
                        break;
                    case api.ExplanationTextItem textElement:
                        replacementValues.Add(textElement.Text);
                        break;
                    case api.ExplanationArticleRefItem articleRef:
                        var firstLemma = articleRef.Lemmas.FirstOrDefault();
                        if (firstLemma != null)
                        {
                            var hgNo = firstLemma.HgNo.ToRomanNumeral();
                            var definitionOrder = articleRef.DefinitionOrder;

                            var pValues = new List<string>();

                            if (!string.IsNullOrWhiteSpace(hgNo))
                                pValues.Add(hgNo);
                            if (definitionOrder > 0)
                                pValues.Add(definitionOrder.ToString());

                            var displayValue = firstLemma.Value;

                            if (pValues.Count > 0)
                            {
                                displayValue = $"{displayValue} ({string.Join(",", pValues)})";
                            }

                            replacementValues.Add(displayValue);
                        }

                        break;
                }
            }

            var finalContentString = ReplaceContentVariables(contentString, replacementValues);

            return finalContentString;
        }

        public string GetExampleContent(api.Example example, string dictionary)
        {
            var contentString = example.Quote.Content;

            var replacementValues = new List<string>();

            foreach (var item in example.Quote.QuoteItems)
            {
                switch (item)
                {
                    case api.QuoteIdItem idElement:
                        var localizedIdItem = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);
                        replacementValues.Add(localizedIdItem);
                        break;
                    case api.QuoteTextItem textElement:
                        replacementValues.Add(textElement.Text);
                        break;
                }
            }

            var finalContentString = ReplaceContentVariables(contentString, replacementValues);

            return finalContentString;
        }

        private static string ReplaceContentVariables(string contentString, List<string> values)
        {
            var contentHasVariables = values.Any();

            if (!contentHasVariables)
                return contentString;

            var regex = new Regex(Regex.Escape("$"));

            values.ForEach(v => contentString = regex.Replace(contentString, v, 1));

            return contentString;
        }
    }
}
