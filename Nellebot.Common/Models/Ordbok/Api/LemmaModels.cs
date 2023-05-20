using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Api;

public record Lemma
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("lemma")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("hgno")]
    public int HgNo { get; set; }

    [JsonPropertyName("initial_lexeme")]
    public string InitialLexeme { get; set; } = string.Empty;

    [JsonPropertyName("final_lexeme")]
    public string FinalLexeme { get; set; } = string.Empty;

    [JsonPropertyName("paradigm_info")]
    public List<Paradigm> Paradigms { get; set; } = new List<Paradigm>();
}

public record Inflection
{
    [JsonPropertyName("word_form")]
    public string WordForm { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();
}

public record Paradigm
{
    [JsonPropertyName("paradigm_id")]
    public int ParadigmId { get; set; }

    [JsonPropertyName("inflection_group")]
    public string InflectionGroup { get; set; } = string.Empty;

    [JsonPropertyName("standardisation")]
    public string Standardisation { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();

    [JsonPropertyName("from")]
    public DateTime? From { get; set; }

    [JsonPropertyName("to")]
    public DateTime? To { get; set; }

    [JsonPropertyName("inflection")]
    public List<Inflection> Inflection { get; set; } = new List<Inflection>();
}

public record SimpleLemma
{
    [JsonPropertyName("lemma")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("hgno")]
    public int HgNo { get; set; }
}
