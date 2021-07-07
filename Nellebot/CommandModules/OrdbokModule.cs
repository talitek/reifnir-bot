using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nellebot.CommandHandlers.Ordbok.SearchOrdbok;

namespace Nellebot.CommandModules
{
    [BaseCommandCheck]
    public class OrdbokModule : BaseCommandModule
    {
        private readonly CommandQueue _commandQueue;

        public OrdbokModule(CommandQueue commandQueue)
        {
            _commandQueue = commandQueue;
        }

        [Command("bm")]
        public Task OrdbokSearchBm(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdBokRequest(ctx)
            {
                Dictionary = "bob",
                Query = query
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

    }
}
