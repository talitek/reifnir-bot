using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using vm = Nellebot.Common.Models.Ordbok.ViewModels;
using api = Nellebot.Common.Models.Ordbok.Api;

namespace Nellebot.CommandHandlers.Ordbok
{
    public class SearchOrdbok
    {
        public class SearchOrdbokRequest : CommandRequest
        {
            public string Query { get; set; } = string.Empty;
            public string Dictionary { get; set; } = string.Empty;
            public bool AttachTemplate { get; set; } = false;

            public SearchOrdbokRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class SearchOrdbokHandler : AsyncRequestHandler<SearchOrdbokRequest>
        {
            private readonly OrdbokHttpClient _ordbokClient;
            private readonly OrdbokModelMapper _ordbokModelMapper;
            private readonly ScribanTemplateLoader _templateLoader;
            private readonly HtmlToImageService _htmlToImageService;
            private readonly ILogger<SearchOrdbokHandler> _logger;

            public SearchOrdbokHandler(
                OrdbokHttpClient ordbokClient,
                OrdbokModelMapper ordbokModelMapper,
                ScribanTemplateLoader templateLoader,
                HtmlToImageService htmlToImageService,
                ILogger<SearchOrdbokHandler> logger
                )
            {
                _ordbokClient = ordbokClient;
                _ordbokModelMapper = ordbokModelMapper;
                _templateLoader = templateLoader;
                _htmlToImageService = htmlToImageService;
                _logger = logger;
            }

            protected override async Task Handle(SearchOrdbokRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;
                var query = request.Query;
                var dictionary = request.Dictionary;
                var attachTemplate = request.AttachTemplate;

                var searchResponse = await _ordbokClient.Search(request.Dictionary, query);

                var articleIds = dictionary == OrdbokDictionaryMap.Bokmal ? searchResponse?.BokmalArticleIds : searchResponse?.NynorskArticleIds;

                if (articleIds == null || articleIds.Count == 0)
                {
                    await ctx.RespondAsync("No match");
                    return;
                }

                var ordbokArticles = await _ordbokClient.GetArticles(dictionary, articleIds);

                var articles = MapAndSelectArticles(query, ordbokArticles);

                var queryUrl = $"https://ordbok.uib.no/?OPP={query}";

                string textTemplateResult = await RenderTextTemplate(articles);

                string htmlTemplateResult = await RenderHtmlTemplate(dictionary, articles);

                var truncatedContent = textTemplateResult.Substring(0, Math.Min(textTemplateResult.Length, DiscordConstants.MaxEmbedContentLength));

                var eb = new DiscordEmbedBuilder()
                    .WithTitle(dictionary == OrdbokDictionaryMap.Bokmal ? "Bokmålsordboka" : "Nynorskordboka")
                    .WithUrl(queryUrl)
                    .WithDescription(truncatedContent)
                    .WithFooter("Universitetet i Bergen og Språkrådet - ordbok.uib.no")
                    .WithColor(DiscordConstants.EmbedColor);

                var mb = new DiscordMessageBuilder();

                FileStream? imageFileStream = null;
                FileStream? htmlFileStream = null;

                try
                {
                    var result = await _htmlToImageService.GenerateImageFile(htmlTemplateResult);

                    imageFileStream = result.ImageFileStream;
                    htmlFileStream = result.HtmlFileStream;

                    if (!attachTemplate)
                    {
                        eb = eb.WithImageUrl($"attachment://{result.ImageFileName}");
                        mb = mb.WithFile(result.ImageFileName, result.ImageFileStream);
                    }
                    else
                    {
                        mb = mb.WithFile(result.HtmlFileStream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, typeof(SearchOrdbok).FullName);
                }

                mb = mb.WithEmbed(eb.Build());

                await ctx.RespondAsync(mb);

                if (imageFileStream != null)
                    await imageFileStream.DisposeAsync();

                if (htmlFileStream != null)
                    await htmlFileStream.DisposeAsync();
            }

            private async Task<string> RenderTextTemplate(List<Common.Models.Ordbok.ViewModels.Article> articles)
            {
                var textTemplateSource = await _templateLoader.LoadTemplate("OrdbokArticle", ScribanTemplateType.Text);
                var textTemplate = Template.Parse(textTemplateSource);
                var textTemplateResult = textTemplate.Render(new { articles });
                return textTemplateResult;
            }

            private async Task<string> RenderHtmlTemplate(string dictionary, List<Common.Models.Ordbok.ViewModels.Article> articles)
            {
                var htmlTemplateSource = await _templateLoader.LoadTemplate("OrdbokArticle", ScribanTemplateType.Html);
                var htmlTemplate = Template.Parse(htmlTemplateSource);
                var htmlTemplateResult = htmlTemplate.Render(new { articles, dictionary });
                return htmlTemplateResult;
            }

            private List<vm.Article> MapAndSelectArticles(string query, List<api.Article?> ordbokArticles)
            {
                var allArticles = ordbokArticles
                    .Where(a => a != null)
                    .Select(_ordbokModelMapper.MapArticle!).ToList();

                // Try to grab exact matches
                var articles = allArticles.Where(x => x.Lemmas.Any(l => l.Value.Equals(query, StringComparison.OrdinalIgnoreCase))).ToList();

                if (articles.Count == 0)
                {
                    articles = allArticles.Take(5).ToList();
                }

                articles = articles.OrderBy(a => a.Lemmas.Max(l => l.HgNo)).ToList();
                return articles;
            }
        }
    }
}
