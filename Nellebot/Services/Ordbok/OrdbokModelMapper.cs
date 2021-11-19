using System.Collections.Generic;
using System.Linq;
using vm = Nellebot.Common.Models.Ordbok.ViewModels;
using api = Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Common.Extensions;

namespace Nellebot.Services.Ordbok
{
    public class OrdbokModelMapper
    {
        private readonly IOrdbokContentParser _contentParser;

        public OrdbokModelMapper(IOrdbokContentParser contentParser)
        {
            _contentParser = contentParser;
        }

        public vm.Article MapArticle(api.Article article)
        {
            var vmResult = new vm.Article();

            var dictionary = article.Dictionary;

            vmResult.ArticleId = article.ArticleId;
            vmResult.Lemmas = article.Lemmas.Select(MapLemma).ToList();

            vmResult.Definitions = MapDefinitions(article.Body.DefinitionElements, dictionary);

            vmResult.SubArticles = MapSubArticles(article.Body.DefinitionElements, dictionary);

            vmResult.Etymologies = MapEtymologies(article.Body.EtymologyGroups, dictionary);

            return vmResult;
        }

        public vm.Lemma MapLemma(api.Lemma lemma)
        {
            var vmResult = new vm.Lemma();

            vmResult.Id = lemma.Id;
            vmResult.Value = lemma.Value;
            vmResult.HgNo = lemma.HgNo;
            vmResult.HgNoRoman = lemma.HgNo.ToRomanNumeral();
            vmResult.Paradigms = lemma.Paradigms.Select(MapParadigm).ToList();

            return vmResult;
        }

        public vm.Paradigm MapParadigm(api.Paradigm paradigm)
        {
            var vmResult = new vm.Paradigm();

            if (!string.IsNullOrWhiteSpace(paradigm.InflectionGroup))
            {
                // TODO fix this
                vmResult.Value = paradigm.InflectionGroup.ToLower() switch
                {
                    // TODO figure out how to differentiate between n1/n2, etc.
                    "noun" => $"{paradigm.Tags[1].ToLower()[0]}", 
                    "noun_regular" => $"{paradigm.Tags[1].ToLower()[0]}",
                    "verb" => "v2",
                    "verb_regular" => "v2",
                    // should be a1,a2 probably
                    "adj_regular" => "adj.",
                    "adj" => "adj.",
                    "adv" => "adv.",
                    "det_simple" => "det.",
                    "pron" => "pron.",
                    "sym" => "symb.",
                    "intj" => "interj.",
                    "abbr"=> "fork.",
                    _ => $"?{paradigm.InflectionGroup.ToLower()}?"
                };
            }

            return vmResult;
        }

        public List<vm.Definition> MapDefinitions(List<api.DefinitionElement> definitionElements, string dictionary)
        {
            var vmResult = new List<vm.Definition>();

            // TODO go recursive if a new nested level is discovered
            foreach (var definitionElement in definitionElements)
            {
                // Top level element is always a Definition (hopefully)
                var definition = (api.Definition)definitionElement;

                var childrenAreDefinitions = definition.DefinitionElements.All(de => de is api.Definition);

                if (childrenAreDefinitions)
                {
                    var nestedDefinitions = definition.DefinitionElements.Cast<api.Definition>().ToList();

                    foreach (var nestedDefinition in nestedDefinitions)
                    {
                        var elementsAreNotSubArticles = !nestedDefinition.DefinitionElements.Any(x => x is api.DefinitionSubArticle);

                        if (!elementsAreNotSubArticles)
                            continue;

                        var mappedNestedDefinition = MapDefinition(nestedDefinition, dictionary);

                        var innerDefinitions = nestedDefinition.DefinitionElements
                            .Where(d => d is api.Definition)
                            .Cast<api.Definition>()
                            .Select(d => MapDefinition(d, dictionary))
                            .ToList();

                        mappedNestedDefinition.InnerDefinitions.AddRange(innerDefinitions);

                        vmResult.Add(mappedNestedDefinition);
                    }
                }
                else
                {
                    var elementsAreNotSubArticles = !definition.DefinitionElements.Any(x => x is api.DefinitionSubArticle);

                    if (!elementsAreNotSubArticles)
                        continue;

                    var mappedDefinition = MapDefinition(definition, dictionary);

                    vmResult.Add(mappedDefinition);
                }
            }

            return vmResult;
        }

        public List<vm.SubArticle> MapSubArticles(List<api.DefinitionElement> definitionElements, string dictionary)
        {
            var vmResult = new List<vm.SubArticle>();

            // TODO go recursive if a new nested level is discovered
            foreach (var definitionElement in definitionElements)
            {
                // Top level element is always a Definition (hopefully)
                var definition = (api.Definition)definitionElement;

                var childrenAreDefinitions = definition.DefinitionElements.All(de => de is api.Definition);

                if (childrenAreDefinitions)
                {
                    var nestedDefinitions = definition.DefinitionElements.Cast<api.Definition>().ToList();

                    foreach (var nestedDefinition in nestedDefinitions)
                    {
                        var elementsAreSubArticles = nestedDefinition.DefinitionElements.All(x => x is api.DefinitionSubArticle);

                        if (!elementsAreSubArticles)
                            continue;

                        var nestedDefinitionAsSubArticles = nestedDefinition.DefinitionElements
                            .Cast<api.DefinitionSubArticle>()
                            .ToList();

                        var mappedSubArticles = nestedDefinitionAsSubArticles.Select(x => MapDefinitionSubArticle(x, dictionary));

                        vmResult.AddRange(mappedSubArticles);
                    }
                }
                else
                {
                    var elementsAreSubArticles = definition.DefinitionElements.All(x => x is api.DefinitionSubArticle);

                    if (!elementsAreSubArticles)
                        continue;

                    var nestedDefinitionAsSubArticles = definition.DefinitionElements
                        .Cast<api.DefinitionSubArticle>()
                        .ToList();

                    var mappedSubArticles = nestedDefinitionAsSubArticles.Select(x => MapDefinitionSubArticle(x, dictionary));

                    vmResult.AddRange(mappedSubArticles);
                }
            }

            return vmResult;
        }

        public vm.Definition MapDefinition(api.Definition definition, string dictionary)
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

            vmResult.Explanations = explanations.Select(x => _contentParser.GetExplanationContent(x, dictionary)).ToList();
            vmResult.Examples = examples.Select(x => _contentParser.GetExampleContent(x, dictionary)).ToList();

            return vmResult;
        }

        public vm.SubArticle MapDefinitionSubArticle(api.DefinitionSubArticle subArticle, string dictionary)
        {
            var vmResult = new vm.SubArticle();

            if (subArticle.Article?.Body == null)
                return vmResult;

            vmResult.Lemmas = subArticle.Article.Body.Lemmas.Select(MapLemma).ToList();

            vmResult.Definitions = MapDefinitions(subArticle.Article.Body.DefinitionElements, dictionary)
                .ToList();

            return vmResult;
        }

        public List<vm.Etymology> MapEtymologies(List<api.EtymologyGroup> etymologyGroups, string dictionary)
        {
            var vmResult = new List<vm.Etymology>();

            foreach (var etymologyGroup in etymologyGroups)
            {
                var vmEtymology = new vm.Etymology();

                switch (etymologyGroup)
                {
                    case api.EtymologyLanguage etymologyLanguage:
                        vmEtymology.Content = _contentParser.GetEtymologyLanguageContent(etymologyLanguage, dictionary);
                        break;
                    case api.EtymologyLitt etymologyLitt:
                        vmEtymology.Content = _contentParser.GetEtymologyLittContent(etymologyLitt, dictionary);
                        break;
                    case api.EtymologyReference etymologyReference:
                        vmEtymology.Content = _contentParser.GetEtymologyReferenceContent(etymologyReference, dictionary);
                        break;
                };

                vmResult.Add(vmEtymology);
            }

            return vmResult;
        }

        
    }
}