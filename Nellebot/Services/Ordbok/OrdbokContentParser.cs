
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

            var contentHasVariables = etymologyLanguage.EtymologyLanguageElements.Any();

            if (!contentHasVariables)
                return contentString;

            var regex = new Regex(Regex.Escape("$"));

            foreach (var item in etymologyLanguage.EtymologyLanguageElements)
            {
                switch (item)
                {
                    case api.EtymologyLanguageIdElement idElement:
                        var localizedIdElement = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);

                        contentString = regex.Replace(contentString, localizedIdElement, 1);
                        break;
                    case api.EtymologyLanguageTextElement textElement:
                        contentString = regex.Replace(contentString, textElement.Text, 1);
                        break;
                }
            }

            return contentString;
        }

        public string GetEtymologyLittContent(api.EtymologyLitt etymologyLitt, string dictionary)
        {
            var contentString = etymologyLitt.Content;

            var contentHasVariables = etymologyLitt.EtymologyLittElements.Any();

            if (!contentHasVariables)
                return contentString;

            var regex = new Regex(Regex.Escape("$"));

            foreach (var item in etymologyLitt.EtymologyLittElements)
            {
                switch (item)
                {
                    case api.EtymologyLittIdElement idElement:
                        var localizedIdElement = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);

                        contentString = regex.Replace(contentString, localizedIdElement, 1);
                        break;
                    case api.EtymologyLittTextElement textElement:
                        contentString = regex.Replace(contentString, textElement.Text, 1);
                        break;
                }
            }

            return contentString;
        }

        public string GetEtymologyReferenceContent(api.EtymologyReference reference, string dictionary)
        {
            var contentString = reference.Content;

            var contentHasVariables = reference.EtymologyReferenceElements.Any();

            if (!contentHasVariables)
                return contentString;

            var regex = new Regex(Regex.Escape("$"));

            foreach (var item in reference.EtymologyReferenceElements)
            {
                switch (item)
                {
                    case api.EtymologyReferenceIdElement idElement:
                        var localizedElementId = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);

                        contentString = regex.Replace(contentString, localizedElementId, 1);
                        break;
                    case api.EtymologyReferenceArticleRef articleRef:
                        var firstLemma = articleRef.Lemmas.FirstOrDefault();
                        if (firstLemma != null)
                        {
                            var value = firstLemma.Value;
                            var hgNo = firstLemma.HgNo.ToRomanNumeral();

                            var showHgNo = !string.IsNullOrWhiteSpace(hgNo);

                            var displayValue = showHgNo ? $"{value} ({hgNo})" : value;

                            contentString = regex.Replace(contentString, displayValue, 1);
                        }

                        break;
                }
            }

            return contentString;
        }

        public string GetExplanationContent(api.Explanation explanation, string dictionary)
        {
            var contentString = explanation.Content;

            var contentHasVariables = explanation.ExplanationItems.Any();

            if (!contentHasVariables)
                return contentString;

            var regex = new Regex(Regex.Escape("$"));

            foreach (var item in explanation.ExplanationItems)
            {
                switch (item)
                {
                    case api.ExplanationIdElement idElement:
                        var localizedElementId = _localizationService.GetString(idElement.Id, LocalizationResource.OrdbokConcepts, dictionary);

                        contentString = regex.Replace(contentString, localizedElementId, 1);
                        break;
                    case api.ExplanationTextElement textElement:
                        contentString = regex.Replace(contentString, textElement.Text, 1);
                        break;
                    case api.ExplanationItemArticleRef articleRef:
                        var firstLemma = articleRef.Lemmas.FirstOrDefault();
                        if (firstLemma != null)
                        {
                            var value = firstLemma.Value;
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

                            contentString = regex.Replace(contentString, displayValue, 1);
                        }

                        break;
                }
            }

            return contentString;
        }
    }
}
