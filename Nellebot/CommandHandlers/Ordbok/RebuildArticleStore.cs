using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using MediatR;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Common.Models.Ordbok.Store;
using Nellebot.Data.Repositories;
using Nellebot.Services.Ordbok;

namespace Nellebot.CommandHandlers.Ordbok;

public record RebuildArticleStoreCommand : BotCommandCommand
{
    public RebuildArticleStoreCommand(CommandContext ctx)
        : base(ctx) { }
}

public class RebuildArticleStoreHandler : IRequestHandler<RebuildArticleStoreCommand>
{
    private static readonly List<string> Dictionaries = new List<string> { OrdbokDictionaryMap.Bokmal, OrdbokDictionaryMap.Nynorsk };
    private static readonly List<string> WordClasses = new List<string> { "NOUN", "VERB" };

    private readonly OrdbokHttpClient _ordbokClient;
    private readonly OrdbokRepository _ordbokRepo;

    public RebuildArticleStoreHandler(
        OrdbokHttpClient ordbokClient,
        OrdbokRepository ordbokRepo)
    {
        _ordbokClient = ordbokClient;
        _ordbokRepo = ordbokRepo;
    }

    public async Task Handle(RebuildArticleStoreCommand request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var channel = request.Ctx.Channel;

        await ctx.RespondAsync("Rebuilding Ordbok article store");

        foreach (var dictionary in Dictionaries)
        {
            foreach (var wordClass in WordClasses)
            {
                var message = await channel.SendMessageAsync($"Downloading {dictionary} {wordClass} articles...");

                try
                {
                    var articles = (await _ordbokClient.GetAll(dictionary, wordClass, cancellationToken)) ?? throw new Exception("Result is null");

                    var articleStore = new OrdbokArticleStore
                    {
                        Dictionary = dictionary,
                        WordClass = wordClass,
                        ArticleCount = articles.Meta[dictionary]?.Total ?? 0,
                        ArticleList = articles.Articles[dictionary] ?? new List<int>(),
                    };

                    await _ordbokRepo.SaveArticleStore(articleStore, cancellationToken);

                    await message.ModifyAsync($"{message.Content} Done.");
                }
                catch (Exception ex)
                {
                    await message.ModifyAsync($"{message.Content} Failed: {ex.Message}");
                    continue;
                }
            }
        }

        foreach (var dictionary in Dictionaries)
        {
            var message = await channel.SendMessageAsync($"Downloading {dictionary} concepts...");

            try
            {
                var concepts = (await _ordbokClient.GetConcepts(dictionary, cancellationToken)) ?? throw new Exception("Result is null");

                var conceptStore = new OrdbokConceptStore
                {
                    Dictionary = dictionary,
                    Concepts = concepts.Concepts.ToDictionary(x => x.Key, x => x.Value.Expansion),
                };

                await _ordbokRepo.SaveConceptStore(conceptStore, cancellationToken);

                await message.ModifyAsync($"{message.Content} Done.");
            }
            catch (Exception ex)
            {
                await message.ModifyAsync($"{message.Content} Failed: {ex.Message}");
                continue;
            }
        }

        await ctx.RespondAsync("All done!");
    }
}
