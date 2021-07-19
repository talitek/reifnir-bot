using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;
using System.Threading.Tasks;
using static Nellebot.CommandHandlers.Ordbok.SearchOrdbok;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class OrdbokModule : BaseCommandModule
    {
        private readonly CommandQueue _commandQueue;

        public OrdbokModule(CommandQueue commandQueue)
        {
            _commandQueue = commandQueue;
        }

        [Command("bm")]
        public Task OrdbokSearchBokmal(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdbokRequest(ctx)
            {
                Dictionary = OrdbokDictionaryMap.Bokmal,
                Query = query
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

        [Command("nn")]
        public Task OrdbokSearchNynorsk(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdbokRequest(ctx)
            {
                Dictionary = OrdbokDictionaryMap.Nynorsk,
                Query = query
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

        [Command("bm-t")]
        public Task OrdbokSearchBokmalDebugTemplate(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdbokRequest(ctx)
            {
                Dictionary = OrdbokDictionaryMap.Bokmal,
                Query = query,
                AttachTemplate = true
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

        [Command("nn-t")]
        public Task OrdbokSearchNynorskDebugTemplate(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdbokRequest(ctx)
            {
                Dictionary = OrdbokDictionaryMap.Nynorsk,
                Query = query,
                AttachTemplate = true
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

    }
}
