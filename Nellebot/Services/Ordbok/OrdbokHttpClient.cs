using Microsoft.Extensions.Options;
using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nellebot.Services.Ordbok
{
    public class OrdbokHttpClient
    {
        private readonly BotOptions _options;
        private readonly HttpClient _client;

        public OrdbokHttpClient(IOptions<BotOptions> options, HttpClient client)
        {
            _options = options.Value;
            _client = client;

            _client.BaseAddress = new Uri("https://beta.ordbok.uib.no/api/");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _client.DefaultRequestHeaders.Add("x-api-key", _options.OrdbokApiKey);
        }

        public async Task<OrdbokSearchResponse?> Search(string dictionary, string query)
        {
            var requestUri = $"dict/{dictionary}/article/search?q={query}";

            var response = await _client.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync();

            var searchResponse = await JsonSerializer.DeserializeAsync<OrdbokSearchResponse>(jsonStream);

            return searchResponse;
        }
    }


}
