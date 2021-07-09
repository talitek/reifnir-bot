using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok.ViewModels
{
    public class Article
    {
        public int ArticleId { get; set; }
        public double Score { get; set; }        
        public List<Lemma> Lemmas { get; set; } = new List<Lemma>();
        public List<Definition> Definitions { get; set; } = new List<Definition>();
        public List<EtymologyLanguage> EtymologyLanguages { get; set; } = new List<EtymologyLanguage>();
        public List<EtymologyReference> EtymologyReferences { get; set; } = new List<EtymologyReference>();
    }

    public class Lemma
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public string HgNo { get; set; } = string.Empty;
        public List<Paradigm> Paradigms { get; set; } = new List<Paradigm>();
    }

    public class Paradigm
    {
        public string Value { get; set; } = string.Empty;
        //public List<Inflection> Inflection { get; set; } = new List<Inflection>();
    }

    public class Definition
    {
        public List<string> Explanations { get; set; } = new List<string>();
        public List<string> Examples { get; set; } = new List<string>();
    }

    public class EtymologyLanguage
    {
        public string Content { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public List<string> Usages { get; set; } = new List<string>();
    }

    public class EtymologyReference
    {
        public string Content { get; set; } = string.Empty;
        public string Relation { get; set; } = string.Empty;
        public string ArticleRef { get; set; } = string.Empty;
    }
}
