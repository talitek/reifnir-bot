﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Nellebot.Attributes;
using Nellebot.Common.Models.Ordbok;
using Nellebot.Workers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nellebot.CommandHandlers.Ordbok.SearchOrdbok;
using static Nellebot.CommandHandlers.Ordbok.SearchOrdbokDebug;

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

        [Command("bm-d")]
        public Task OrdbokSearchBokmalDebug(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdbokDebugRequest(ctx)
            {
                Dictionary = OrdbokDictionaryMap.Bokmal,
                Query = query
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

        [Command("nn-d")]
        public Task OrdbokSearchNynorskDebug(CommandContext ctx, [RemainingText] string query)
        {
            var searchOrdbokRequest = new SearchOrdbokDebugRequest(ctx)
            {
                Dictionary = OrdbokDictionaryMap.Nynorsk,
                Query = query
            };

            _commandQueue.Enqueue(searchOrdbokRequest);

            return Task.CompletedTask;
        }

    }
}