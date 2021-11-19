using Nellebot.Common.Models.Ordbok.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok.Api
{
    public class OrdbokSearchResponse 
    {
        [JsonPropertyName("articles")]
        public Articles Articles { get; set; } = null!;
    }

    public class Articles
    {
        [JsonPropertyName("bm")]
        public List<int>? BokmalArticleIds { get; set; }

        [JsonPropertyName("nn")]
        public List<int>? NynorskArticleIds { get; set; }
    }
}
