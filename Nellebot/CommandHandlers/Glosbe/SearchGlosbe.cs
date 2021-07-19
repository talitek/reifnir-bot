using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using Nellebot.Services;
using Nellebot.Services.Glosbe;
using Nellebot.Utils;
using Scriban;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nellebot.CommandHandlers.Glosbe
{
    public class SearchGlosbe
    {
        public class SearchGlosbeRequest : CommandRequest
        {
            public string Query { get; set; } = string.Empty;
            public string OriginalLanguage { get; set; } = string.Empty;
            public string TargetLanguage { get; set; } = string.Empty;

            public SearchGlosbeRequest(CommandContext ctx) : base(ctx)
            {
            }
        }

        public class SearchGlosbeHandler : AsyncRequestHandler<SearchGlosbeRequest>
        {
            private readonly GlosbeClient _glosbeClient;
            private readonly GlosbeModelMapper _glosbeModelMapper;
            private readonly ScribanTemplateLoader _templateLoader;
            private readonly ILogger<SearchGlosbeHandler> _logger;

            public SearchGlosbeHandler(
                GlosbeClient glosbeClient,
                GlosbeModelMapper glosbeModelMapper,
                ScribanTemplateLoader templateLoader,
                ILogger<SearchGlosbeHandler> logger
                )
            {
                _glosbeClient = glosbeClient;
                _glosbeModelMapper = glosbeModelMapper;
                _templateLoader = templateLoader;
                _logger = logger;
            }

            protected override async Task Handle(SearchGlosbeRequest request, CancellationToken cancellationToken)
            {
                var ctx = request.Ctx;
                var query = request.Query;
                var orignalLanguage = request.OriginalLanguage;
                var targetLanguage = request.TargetLanguage;

                var translationResult = await _glosbeClient.GetTranslation(orignalLanguage, targetLanguage, query);

                var model = _glosbeModelMapper.MapTranslationResult(translationResult);

                var textTemplateSource = await _templateLoader.LoadTemplate("GlosbeArticle", ScribanTemplateType.Text);
                var textTemplate = Template.Parse(textTemplateSource);
                var textTemplateResult = textTemplate.Render(new { model.Article });

                var trimmedResult = textTemplateResult.Trim();
                var truncatedContent = trimmedResult.Substring(0, Math.Min(trimmedResult.Length, DiscordConstants.MaxEmbedContentLength));

                var eb = new DiscordEmbedBuilder()
                    .WithTitle("Glosbe")
                    .WithUrl(model.QueryUrl)
                    .WithDescription(truncatedContent)
                    .WithFooter("Glosbe.com")
                    .WithColor(DiscordConstants.EmbedColor);

                await ctx.RespondAsync(eb.Build());
            }
        }
    }
}
