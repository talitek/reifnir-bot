using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using api = Nellebot.Common.Models.Ordbok.Api;

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

                var modelMapper = new OrdbokModelMapper(ordbokContentParser, localizationService.Object);

                var article = modelMapper.MapArticle(result!, "bm");
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
