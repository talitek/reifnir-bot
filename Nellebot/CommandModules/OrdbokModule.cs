using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Ordbok;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;

namespace Nellebot.CommandModules;

[BaseCommandCheck]
[ModuleLifespan(ModuleLifespan.Transient)]
public class OrdbokModule : BaseCommandModule
{
    private readonly RequestQueueChannel _requestQueue;

    public OrdbokModule(RequestQueueChannel commandQueue)
    {
        _requestQueue = commandQueue;
    }

    [Command("bm")]
    [Aliases("nb")]
    public Task OrdbokSearchBokmal(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [Command("nn")]
    public Task OrdbokSearchNynorsk(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [Command("bm-t")]
    public Task OrdbokSearchBokmalDebugTemplate(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Bokmal,
            Query = query,
            AttachTemplate = true,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [Command("nn-t")]
    public Task OrdbokSearchNynorskDebugTemplate(CommandContext ctx, [RemainingText] string query)
    {
        var searchOrdbokRequest = new SearchOrdbokQuery(ctx)
        {
            Dictionary = OrdbokDictionaryMap.Nynorsk,
            Query = query,
            AttachTemplate = true,
        };

        return _requestQueue.Writer.WriteAsync(searchOrdbokRequest).AsTask();
    }

    [Command("gibb")]
    public Task Gibb(CommandContext ctx)
    {
        var gibbCommand = new GibbCommand(ctx);

        return _requestQueue.Writer.WriteAsync(gibbCommand).AsTask();
    }
}
