using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok
{
    public class OrdbokSearchRequest
    {
        [JsonPropertyName("dictionary_id")]
        public string DictionaryId { get; set; } = string.Empty;
        [JsonPropertyName("q")]
        public string Query { get; set; } = string.Empty;
    }
}
