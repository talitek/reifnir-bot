﻿using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MediatR;
using Nellebot.Services.Ordbok;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers.Ordbok
{
    public class SearchOrdbokDebug
    {
        public class SearchOrdbokDebugRequest : CommandRequest
        {
            public string Query { get; set; } = string.Empty;
            public string Dictionary { get; set; } = string.Empty;

            public SearchOrdbokDebugRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class SearchOrdbokDebugHandler : AsyncRequestHandler<SearchOrdbokDebugRequest>
        {
            private readonly OrdbokHttpClient _ordbokClient;

            public SearchOrdbokDebugHandler(OrdbokHttpClient ordbokClient)
            {
                _ordbokClient = ordbokClient;
            }

            protected override async Task Handle(SearchOrdbokDebugRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;

                var searchResponse = await _ordbokClient.Search(request.Dictionary, request.Query);

                if (searchResponse == null)
                {
                    await ctx.RespondAsync($"no result");
                    return;
                }

                using var memoryStream = new MemoryStream();
                using var jsonWriter = new JsonTextWriter(new StreamWriter(memoryStream));

                var jsonSerializer = new JsonSerializer() { Formatting = Formatting.Indented };

                jsonSerializer.Serialize(jsonWriter, searchResponse);

                await jsonWriter.FlushAsync();

                memoryStream.Position = 0;

                var responseBuilder = new DiscordMessageBuilder();

                responseBuilder
                    .WithFile($"{request.Dictionary}-{request.Query}-api-output.txt", memoryStream);

                await ctx.RespondAsync(responseBuilder);

            }
        }
    }
}