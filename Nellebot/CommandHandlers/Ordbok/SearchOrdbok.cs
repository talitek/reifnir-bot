using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Common.Models.Ordbok;
using Nellebot.ScribanTemplates;
using Nellebot.Services;
using Scriban;
using Scriban.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers.Ordbok
{
    public class SearchOrdbok
    {
        public class SearchOrdbokRequest : CommandRequest
        {
            public string Query { get; set; } = string.Empty;
            public string Dictionary { get; set; } = string.Empty;

            public SearchOrdbokRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class SearchOrdbokHandler : AsyncRequestHandler<SearchOrdbokRequest>
        {
            private readonly OrdbokHttpClient _ordbokClient;
            private readonly ScribanTemplateLoader _templateLoader;

            public SearchOrdbokHandler(
                OrdbokHttpClient ordbokClient,
                ScribanTemplateLoader templateLoader
                )
            {
                _ordbokClient = ordbokClient;
                _templateLoader = templateLoader;
            }

            protected override async Task Handle(SearchOrdbokRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;
                var query = request.Query;
                var dictionary = request.Dictionary;

                var searchResponse = await _ordbokClient.Search(request.Dictionary, query);

                if (searchResponse == null)
                {
                    await ctx.RespondAsync($"no result");
                    return;
                }

                var allArticles = searchResponse.Select(OrdbokModelMapper.MapArticle).ToList();

                if (allArticles.Count == 0)
                {
                    await ctx.RespondAsync("No match");
                    return;
                }

                // Try to grab exact matches
                var articles = allArticles.Where(x => x.Lemmas.Any(l => l.Value == query)).ToList();

                if(articles.Count == 0)
                {
                    articles = allArticles.Take(5).ToList();
                }

                articles = articles.OrderBy(a => a.Lemmas.Max(l => l.HgNo)).ToList();

                var templateSource = await _templateLoader.LoadTemplate("OrdbokArticle");

                var template = Template.Parse(templateSource);
                var templateResult = template.Render(new { Articles = articles, Dictionary = dictionary });

                await ctx.RespondAsync($"{templateResult.Substring(0, Math.Min(templateResult.Length, 2000))}");
            }
        }
    }
}
