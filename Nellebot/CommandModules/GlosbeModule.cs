using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.CommandHandlers.Glosbe;
using Nellebot.Common.Models.Glosbe;
using Nellebot.Workers;
using System.Threading.Tasks;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GlosbeModule : BaseCommandModule
    {
        private readonly CommandQueue _commandQueue;

        public GlosbeModule(CommandQueue commandQueue)
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
                TargetLanguage = GlosbeLanguageMap.English
            };

            _commandQueue.Enqueue(searchGlosbeRequest);

            return Task.CompletedTask;
        }

        [Command("en-nb")]
        public Task TranslateEnglishToBokmal(CommandContext ctx, [RemainingText] string query)
        {
            var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
            {
                Query = query,
                OriginalLanguage = GlosbeLanguageMap.English,
                TargetLanguage = GlosbeLanguageMap.Bokmal
            };

            _commandQueue.Enqueue(searchGlosbeRequest);

            return Task.CompletedTask;
        }

        [Command("nn-en")]
        public Task TranslateNynorskToEnglish(CommandContext ctx, [RemainingText] string query)
        {
            var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
            {
                Query = query,
                OriginalLanguage = GlosbeLanguageMap.Nynorsk,
                TargetLanguage = GlosbeLanguageMap.English
            };

            _commandQueue.Enqueue(searchGlosbeRequest);

            return Task.CompletedTask;
        }

        [Command("en-nn")]
        public Task TranslateEnglishToNynorsk(CommandContext ctx, [RemainingText] string query)
        {
            var searchGlosbeRequest = new SearchGlosbeRequest(ctx)
            {
                Query = query,
                OriginalLanguage = GlosbeLanguageMap.English,
                TargetLanguage = GlosbeLanguageMap.Nynorsk
            };

            _commandQueue.Enqueue(searchGlosbeRequest);

            return Task.CompletedTask;
        }
    }
}
