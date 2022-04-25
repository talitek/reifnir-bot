using Microsoft.Extensions.Options;
using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nellebot.Services.Ordbok
{
    public class OrdbokHttpClient
    {
        private readonly HttpClient _client;

        private const int _maxArticles = 5;

        public OrdbokHttpClient(HttpClient client)
        {
            _client = client;

            _client.BaseAddress = new Uri("https://ord.uib.no/");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<OrdbokSearchResponse?> Search(string dictionary, string query)
        {
            var requestUri = $"api/articles?w={query}&dict={dictionary}&scope=ei";

            var response = await _client.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync();

            var searchResponse = await JsonSerializer.DeserializeAsync<OrdbokSearchResponse>(jsonStream);

            return searchResponse;
        }

        public async Task<Article?> GetArticle(string dictionary, int articleId)
        {
            var requestUri = $"{dictionary}/article/{articleId}.json";

            var response = await _client.GetAsync(requestUri);

            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync();

            var article = await JsonSerializer.DeserializeAsync<Article>(jsonStream);

            return article;
        }

        public async Task<List<Article?>> GetArticles(string dictionary, List<int> articleIds)
        {
            var tasks = articleIds.Take(_maxArticles).Select(id => GetArticle(dictionary, id));

            var result = await Task.WhenAll(tasks);

            if (result == null)
                return Enumerable.Empty<Article?>().ToList();

            return result.ToList();
        }
    }
}
