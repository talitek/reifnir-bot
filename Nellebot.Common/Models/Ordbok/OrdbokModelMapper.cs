using System.Collections.Generic;
using System.Linq;
using vm = Nellebot.Common.Models.Ordbok.ViewModels;
using api = Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.Common.Models.Ordbok
{
    public static class OrdbokModelMapper
    {
        public static vm.Article MapArticle(api.Article article)
        {
            var vmResult = new vm.Article();

            vmResult.ArticleId = article.ArticleId;
            vmResult.Score = article.Score;
            vmResult.Lemmas = article.Lemmas.Select(MapLemma).ToList();

            vmResult.Definitions = article.Body.DefinitionGroups
                .FirstOrDefault()
                ?.Definitions
                .Select(MapDefinition)
                .ToList()
                ?? Enumerable.Empty<vm.Definition>().ToList();

            vmResult.EtymologyLanguages = MapEtymologyLanguages(article.Body.EtymologyGroups);
            vmResult.EtymologyReferences = MapEtymologyReferences(article.Body.EtymologyGroups);

            return vmResult;
        }

        public static vm.Lemma MapLemma(api.Lemma lemma)
        {
            var vm = new vm.Lemma();

            vm.Id = lemma.Id;
            vm.Value = lemma.Value;
            // TODO do better
            vm.HgNo = lemma.HgNo switch
            {
                0 => string.Empty,
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                _ => "??"
            };
            vm.Paradigms = lemma.Paradigms.Select(MapParadigm).ToList();

            return vm;
        }

        public static vm.Paradigm MapParadigm(api.Paradigm paradigm)
        {
            var vm = new vm.Paradigm();

            if (string.IsNullOrWhiteSpace(paradigm.InflectionGroup))
            {
                vm.Value = paradigm.Standardisation ?? "??";
            }
            else
            {
                // TODO fix this
                vm.Value = paradigm.InflectionGroup.ToLower() switch
                {
                    "verb" => "v2",
                    "adv" => "adv.",
                    // TODO figure out how to differentiate between n1/n2, etc.
                    "noun" => $"{paradigm.Tags[1].ToLower()[0]}1",
                    "det_simple" => "det.",
                    "pron" => "pron.",
                    _ => "??"
                };
            }

            return vm;
        }

        public static vm.Definition MapDefinition(api.Definition definition)
        {
            var vm = new vm.Definition();

            var explanations = definition.DefinitionElements
                .Where(de => de is api.Explanation)
                .Cast<api.Explanation>()
                .ToList();

            var examples = definition.DefinitionElements
                .Where(de => de is api.Example)
                .Cast<api.Example>()
                .ToList();

            vm.Explanations = explanations.Select(e => e.Content).ToList();
            vm.Examples = examples.Select(e => e.Quote.Content).ToList();

            return vm;
        }

        public static List<vm.EtymologyLanguage> MapEtymologyLanguages(List<api.EtymologyGroup> etymologyGroups)
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

        public static List<vm.EtymologyReference> MapEtymologyReferences(List<api.EtymologyGroup> etymologyGroups)
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

                if(referenceArticleRef != null)
                {
                    vmEtymologyReference.Relation = referenceArticleRef.ArticleId.ToString();
                }

                vmResult.Add(vmEtymologyReference);
            }

            return vmResult;
        }
    }
}