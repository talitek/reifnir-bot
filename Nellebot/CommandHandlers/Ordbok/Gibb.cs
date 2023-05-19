using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Data.Repositories;
using Nellebot.Services.Ordbok;

namespace Nellebot.CommandHandlers.Ordbok;

public record GibbCommand : BotCommandCommand
{
    public GibbCommand(CommandContext ctx)
        : base(ctx) { }
}

public class GibbHandler : IRequestHandler<GibbCommand>
{
    private readonly OrdbokRepository _ordbokRepo;
    private readonly OrdbokHttpClient _ordbokClient;

    public GibbHandler(OrdbokRepository ordbokRepo, OrdbokHttpClient ordbokClient)
    {
        _ordbokRepo = ordbokRepo;
        _ordbokClient = ordbokClient;
    }

    public async Task Handle(GibbCommand request, CancellationToken cancellationToken)
    {
        const string dictionary = "bm";
        const string wordClass = "NOUN";

        var ctx = request.Ctx;

        var articleCount = await _ordbokRepo.GetArticleCount(dictionary, wordClass, cancellationToken);

        var random = new Random().Next(articleCount);

        var articleId = await _ordbokRepo.GetArticleIdAtIndex(dictionary, wordClass, random, cancellationToken);

        var article = (await _ordbokClient.GetArticle(dictionary, articleId, cancellationToken))
            ?? throw new Exception($"Couldn't fetch article id {articleId}");

        var message = $"Here's your article ({articleId}): {article.Lemmas.First().Value}";

        await ctx.RespondAsync(message);
    }
}
