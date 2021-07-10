using System.Collections.Generic;
using System.Linq;
using vm = Nellebot.Common.Models.Ordbok.ViewModels;
using api = Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Common.Extensions;
using Nellebot.Common.Models.Ordbok;
using System.Text.RegularExpressions;

namespace Nellebot.Services.Ordbok
{
    public class OrdbokModelMapper
    {
        private readonly LocalizationService _localizationService;

        public OrdbokModelMapper(LocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public vm.Article MapArticle(api.Article article)
        {
            var vmResult = new vm.Article();

            vmResult.ArticleId = article.ArticleId;
            vmResult.Score = article.Score;
            vmResult.Lemmas = article.Lemmas.Select(MapLemma).ToList();

            vmResult.Definitions = MapDefinitions(article.Body.DefinitionElements);

            vmResult.EtymologyLanguages = MapEtymologyLanguages(article.Body.EtymologyGroups);
            vmResult.EtymologyReferences = MapEtymologyReferences(article.Body.EtymologyGroups);

            return vmResult;
        }

        public vm.Lemma MapLemma(api.Lemma lemma)
        {
            var vmResult = new vm.Lemma();

            vmResult.Id = lemma.Id;
            vmResult.Value = lemma.Value;
            // TODO do better
            vmResult.HgNo = lemma.HgNo.ToRomanNumeral();
            vmResult.Paradigms = lemma.Paradigms.Select(MapParadigm).ToList();

            return vmResult;
        }

        public vm.Paradigm MapParadigm(api.Paradigm paradigm)
        {
            var vmResult = new vm.Paradigm();

            if (string.IsNullOrWhiteSpace(paradigm.InflectionGroup))
            {
                vmResult.Value = paradigm.Standardisation ?? "??";
            }
            else
            {
                // TODO fix this
                vmResult.Value = paradigm.InflectionGroup.ToLower() switch
                {
                    "verb" => "v2",
                    "adv" => "adv.",
                    // TODO figure out how to differentiate between n1/n2, etc.
                    //"noun" => $"{paradigm.Tags[1].ToLower()[0]}?1?",
                    "noun" => $"{paradigm.Tags[1].ToLower()[0]}",
                    "det_simple" => "det.",
                    "pron" => "pron.",
                    _ => "??"
                };
            }

            return vmResult;
        }

        public List<vm.Definition> MapDefinitions(List<api.DefinitionElement> definitionElements)
        {
            var vmResult = new List<vm.Definition>();

            foreach (var definitionElement in definitionElements)
            {
                // Top level element is always a Definition (hopefully)
                var definition = (api.Definition)definitionElement;

                var containsDefinitions = definition.DefinitionElements.All(de => de is api.Definition);

                if (containsDefinitions)
                {
                    var nestedDefinitions = definition.DefinitionElements.Cast<api.Definition>().ToList();

                    foreach (var nestedDefinition in nestedDefinitions)
                    {
                        vmResult.Add(MapDefinition(nestedDefinition));
                    }
                }
                else
                {
                    vmResult.Add(MapDefinition(definition));
                }
            }

            return vmResult;
        }

        public vm.Definition MapDefinition(api.Definition definition)
        {
            var vmResult = new vm.Definition();

            var explanations = definition.DefinitionElements
                .Where(de => de is api.Explanation)
                .Cast<api.Explanation>()
                .ToList();

            var examples = definition.DefinitionElements
                .Where(de => de is api.Example)
                .Cast<api.Example>()
                .ToList();

            vmResult.Explanations = explanations.Select(e => GetExplanationContent(e)).ToList();
            vmResult.Examples = examples.Select(e => e.Quote.Content).ToList();

            return vmResult;
        }

        public List<vm.EtymologyLanguage> MapEtymologyLanguages(List<api.EtymologyGroup> etymologyGroups)
        {
            var vmResult = new List<vm.EtymologyLanguage>();

            var apiEtymologyLanguages = etymologyGroups
                .Where(x => x is api.EtymologyLanguage)
                .Cast<api.EtymologyLanguage>()
                .ToList();

            foreach (var apiEtymologyLanguage in apiEtymologyLanguages)
            {
                var vmEtymologyLanguage = new vm.EtymologyLanguage();

                vmEtymologyLanguage.Content = apiEtymologyLanguage.Content;

                var apiEtymologyLanguageLanguages = apiEtymologyLanguage.EtymologyLanguageElements
                    .Where(x => x is api.EtymologyLanguageLanguage)
                    .Cast<api.EtymologyLanguageLanguage>();

                vmEtymologyLanguage.Language = apiEtymologyLanguageLanguages.FirstOrDefault()?.Id
                                                ?? string.Empty;

                var apiEtymologyLanguageRelations = apiEtymologyLanguage.EtymologyLanguageElements
                    .Where(x => x is api.EtymologyLanguageRelation)
                    .Cast<api.EtymologyLanguageRelation>();

                vmEtymologyLanguage.Relation = apiEtymologyLanguageRelations.FirstOrDefault()?.Id
                                                ?? string.Empty;

                var apiEtymologyLanguageUsages = apiEtymologyLanguage.EtymologyLanguageElements
                    .Where(x => x is api.EtymologyLanguageUsage)
                    .Cast<api.EtymologyLanguageUsage>();

                vmEtymologyLanguage.Usages = apiEtymologyLanguageUsages.Select(x => x.Text).ToList();

                vmResult.Add(vmEtymologyLanguage);
            }

            return vmResult;
        }

        public List<vm.EtymologyReference> MapEtymologyReferences(List<api.EtymologyGroup> etymologyGroups)
        {
            var vmResult = new List<vm.EtymologyReference>();

            var apiEtymologyReferences = etymologyGroups
                .Where(x => x is api.EtymologyReference)
                .Cast<api.EtymologyReference>()
                .ToList();

            foreach (var apiEtymologyReference in apiEtymologyReferences)
            {
                var vmEtymologyReference = new vm.EtymologyReference();

                vmEtymologyReference.Content = apiEtymologyReference.Content;

                var apiEtymologyReferenceRelations = apiEtymologyReference.EtymologyReferenceElements
                    .Where(x => x is api.EtymologyReferenceRelation)
                    .Cast<api.EtymologyReferenceRelation>();

                vmEtymologyReference.Relation = apiEtymologyReferenceRelations.FirstOrDefault()?.Id
                                                ?? string.Empty;

                var apiEtymologyReferenceArticleRefs = apiEtymologyReference.EtymologyReferenceElements
                    .Where(x => x is api.EtymologyReferenceArticleRef)
                    .Cast<api.EtymologyReferenceArticleRef>();

                var referenceArticleRef = apiEtymologyReferenceArticleRefs.FirstOrDefault();

                if (referenceArticleRef != null)
                {
                    vmEtymologyReference.Relation = referenceArticleRef.ArticleId.ToString();
                }

                vmResult.Add(vmEtymologyReference);
            }

            return vmResult;
        }

        public string GetExplanationContent(api.Explanation explanation)
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
                        var localizedElementId = _localizationService.GetString(LocalizationResource.Ordbok, idElement.Id);

                        // Temporarily mark elements that are missing from localization file
                        if (localizedElementId == idElement.Id)
                            localizedElementId = $"?{localizedElementId}?";

                        contentString = regex.Replace(contentString, localizedElementId, 1);
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