using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Glosbe;
using Nellebot.Common.Models.Glosbe;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[ModuleLifespan(ModuleLifespan.Transient)]
public class GlosbeModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandQueue;

    public GlosbeModule(CommandQueueChannel commandQueue)
    {
        _commandQueue = commandQueue;
    }

    [Command("nb-en")]
    public Task TranslateBokmalToEnglish(CommandContext ctx, [RemainingText] string query)
    {
        var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
        {
            Query = query,
            OriginalLanguage = GlosbeLanguageMap.Bokmal,
            TargetLanguage = GlosbeLanguageMap.English,
        };

        return _commandQueue.Writer.WriteAsync(searchGlosbeRequest).AsTask();
    }

    [Command("en-nb")]
    public Task TranslateEnglishToBokmal(CommandContext ctx, [RemainingText] string query)
    {
        var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
        {
            Query = query,
            OriginalLanguage = GlosbeLanguageMap.English,
            TargetLanguage = GlosbeLanguageMap.Bokmal,
        };

        return _commandQueue.Writer.WriteAsync(searchGlosbeRequest).AsTask();
    }

    [Command("nn-en")]
    public Task TranslateNynorskToEnglish(CommandContext ctx, [RemainingText] string query)
    {
        var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
        {
            Query = query,
            OriginalLanguage = GlosbeLanguageMap.Nynorsk,
            TargetLanguage = GlosbeLanguageMap.English,
        };

        return _commandQueue.Writer.WriteAsync(searchGlosbeRequest).AsTask();
    }

    [Command("en-nn")]
    public Task TranslateEnglishToNynorsk(CommandContext ctx, [RemainingText] string query)
    {
        var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
        {
            Query = query,
            OriginalLanguage = GlosbeLanguageMap.English,
            TargetLanguage = GlosbeLanguageMap.Nynorsk,
        };

        return _commandQueue.Writer.WriteAsync(searchGlosbeRequest).AsTask();
    }
}
