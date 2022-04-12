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
    public class OrdbokTests
    {
        [TestMethod]
        [Ignore]
        public async Task TestArticleDeserialization()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            var file = Path.Combine(directory, "TestFiles/test.json");

            var json = await File.ReadAllTextAsync(file);

            try
            {
                var result = JsonSerializer.Deserialize<api.Article>(json);

                var localizationService = new Mock<ILocalizationService>();

                localizationService
                    .Setup(m => m.GetString(It.IsAny<string>(), It.IsAny<LocalizationResource>(), It.IsAny<string>()))
                    .Returns((string s, LocalizationResource rs, string loc) => s);

                var ordbokContentParser = new OrdbokContentParser(localizationService.Object);

                var modelMapper = new OrdbokModelMapper(ordbokContentParser);

                var article = modelMapper.MapArticle(result!);

            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
