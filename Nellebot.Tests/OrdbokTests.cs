using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using vm = Nellebot.Common.Models.Ordbok.ViewModels;
using api = Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Scriban;
using Nellebot.Services.HtmlToImage;
using Microsoft.Extensions.Logging;

namespace Nellebot.Tests
{
    [TestClass]
    [Ignore]
    public class OrdbokTests
    {
        [TestMethod]
        public async Task TestDeserialization()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            var file = Path.Combine(directory, "TestFiles/test.json");

            var json = await File.ReadAllTextAsync(file);

            try
            {
                var result = JsonSerializer.Deserialize<api.OrdbokSearchResponse>(json);

            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod]
        public async Task TestModelMapping()
        {
            try
            {
                var query = "fly";

                var allArticles = await LoadTestModel();

                var articles = allArticles.Where(x => x.Lemmas.Any(l => l.Value == query)).ToList();

                if (articles.Count == 0)
                {
                    articles = allArticles.Take(5).ToList();
                }

                articles = articles.OrderBy(a => a.Lemmas.Max(l => l.HgNo)).ToList();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod]
        public async Task TestHtmlGenerationMapping()
        {
            var wkHtmlClient = new WkHtmlToImageClient();
            var logger = new Mock<ILogger<HtmlToImageService>>();
            var htmlToImageService = new HtmlToImageService(wkHtmlClient, logger.Object);

            try
            {
                var query = "fly";

                var allArticles = await LoadTestModel();

                var articles = allArticles.Where(x => x.Lemmas.Any(l => l.Value == query)).ToList();

                if (articles.Count == 0)
                {
                    articles = allArticles.Take(5).ToList();
                }

                articles = articles.OrderBy(a => a.Lemmas.Max(l => l.HgNo)).ToList();

                var htmlTemplateSource = await File.ReadAllTextAsync($"TestFiles/TestOrdbokArticle.sbnhtml");
                var htmlTemplate = Template.Parse(htmlTemplateSource);
                var htmlTemplateResult = htmlTemplate.Render(new { Articles = articles, Dictionary = "nob" });

                var imagePath = await htmlToImageService.GenerateImageFile(htmlTemplateResult);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        private async Task<List<vm.Article>> LoadTestModel()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            var file = Path.Combine(directory, "TestFiles/test.json");

            var json = await File.ReadAllTextAsync(file);

            var result = JsonSerializer.Deserialize<api.OrdbokSearchResponse>(json);

            var localizationService = new Mock<ILocalizationService>();

            localizationService
                .Setup(m => m.GetString(It.IsAny<string>(), It.IsAny<LocalizationResource>(), It.IsAny<string>()))
                .Returns((string s, LocalizationResource rs, string loc) => s);

            var ordbokContentParser = new OrdbokContentParser(localizationService.Object);

            var modelMapper = new OrdbokModelMapper(ordbokContentParser);

            var articles = result!.Select(modelMapper.MapArticle).ToList();

            return articles;
        }
    }
}
