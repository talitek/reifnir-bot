using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using Nellebot.Utils;
using Scriban;
using Api = Nellebot.Common.Models.Ordbok.Api;
using Vm = Nellebot.Common.Models.Ordbok.ViewModels;

namespace Nellebot.CommandHandlers.Ordbok;

public record SearchOrdbokQueryV2 : InteractionCommand
{
    public SearchOrdbokQueryV2(InteractionContext ctx)
        : base(ctx)
    {
    }

    public string Query { get; set; } = string.Empty;

    public string Dictionary { get; set; } = string.Empty;
}

public class SearchOrdbokHandlerV2 : IRequestHandler<SearchOrdbokQueryV2>
{
    private const int MaxDefinitionsInTextForm = 5;

    private readonly OrdbokHttpClient _ordbokClient;
    private readonly OrdbokModelMapper _ordbokModelMapper;
    private readonly ScribanTemplateLoader _templateLoader;

    public SearchOrdbokHandlerV2(
        OrdbokHttpClient ordbokClient,
        OrdbokModelMapper ordbokModelMapper,
        ScribanTemplateLoader templateLoader)
    {
        _ordbokClient = ordbokClient;
        _ordbokModelMapper = ordbokModelMapper;
        _templateLoader = templateLoader;
    }

    public async Task Handle(SearchOrdbokQueryV2 request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var query = request.Query;
        var dictionary = request.Dictionary;

        await ctx.DeferAsync();

        var searchResponse = await _ordbokClient.Search(request.Dictionary, query, cancellationToken);

        var articleIds = searchResponse?.Articles[dictionary];

        if (articleIds == null || articleIds.Length == 0)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No match"));
            return;
        }

        var ordbokArticles = await _ordbokClient.GetArticles(dictionary, articleIds.ToList(), cancellationToken);

        var articles = MapAndSelectArticles(ordbokArticles, dictionary);

        var queryUrl = $"https://ordbokene.no/{(dictionary == OrdbokDictionaryMap.Bokmal ? "bm" : "nn")}/w/{query}";

        string textTemplateResult = await RenderTextTemplate(articles);

        var truncatedContent = textTemplateResult[..Math.Min(textTemplateResult.Length, DiscordConstants.MaxEmbedContentLength)];

        var eb = new DiscordEmbedBuilder()
            .WithTitle(dictionary == OrdbokDictionaryMap.Bokmal ? "Bokmålsordboka" : "Nynorskordboka")
            .WithUrl(queryUrl)
            .WithDescription(truncatedContent)
            .WithFooter("Universitetet i Bergen og Språkrådet - ordbokene.no")
            .WithColor(DiscordConstants.DefaultEmbedColor);

        var response = new DiscordWebhookBuilder().AddEmbed(eb.Build());

        await ctx.EditResponseAsync(response);
    }

    private async Task<string> RenderTextTemplate(List<Vm.Article> articles)
    {
        var textTemplateSource = await _templateLoader.LoadTemplate("OrdbokArticleV2", ScribanTemplateType.Text);
        var textTemplate = Template.Parse(textTemplateSource);

        var maxDefinitions = MaxDefinitionsInTextForm;

        var textTemplateResult = textTemplate.Render(new { articles, maxDefinitions });

        return textTemplateResult;
    }

    private List<Vm.Article> MapAndSelectArticles(List<Api.Article?> ordbokArticles, string dictionary)
    {
        var articles = ordbokArticles
            .Where(a => a != null)
            .Select(x => _ordbokModelMapper.MapArticle(x!, dictionary))
            .OrderBy(a => a.Lemmas.Max(l => l.HgNo))
            .ToList();

        return articles;
    }
}
