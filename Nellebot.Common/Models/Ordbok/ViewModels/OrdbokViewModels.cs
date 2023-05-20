using System.Collections.Generic;
using System.Linq;

namespace Nellebot.Common.Models.Ordbok.ViewModels;

public record Article
{
    public int ArticleId { get; set; }

    public List<Lemma> Lemmas { get; set; } = new List<Lemma>();

    public List<Definition> Definitions { get; set; } = new List<Definition>();

    public List<SubArticle> SubArticles { get; set; } = new List<SubArticle>();

    public List<Etymology> Etymologies { get; set; } = new List<Etymology>();
}

public record Lemma
{
    public int Id { get; set; }

    public string Value { get; set; } = string.Empty;

    public int HgNo { get; set; }

    public string HgNoRoman { get; set; } = string.Empty;

    public List<Paradigm> Paradigms { get; set; } = new List<Paradigm>();

    public List<string> UniqueParadigmValues => Paradigms.Select(p => p.Value).Distinct().ToList();
}

public record Paradigm
{
    public string Value { get; set; } = string.Empty;

    ////public List<Inflection> Inflection { get; set; } = new List<Inflection>();
}

public record Definition
{
    public List<string> Explanations { get; set; } = new List<string>();

    public List<string> ExplanationsSimple { get; set; } = new List<string>();

    public List<string> Examples { get; set; } = new List<string>();

    public List<Definition> InnerDefinitions { get; set; } = new List<Definition>();
}

public record SubArticle
{
    public List<Lemma> Lemmas { get; set; } = new List<Lemma>();

    public List<Definition> Definitions { get; set; } = new List<Definition>();
}

public record Etymology
{
    public string Content { get; set; } = string.Empty;
}
