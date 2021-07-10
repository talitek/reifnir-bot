using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Nellebot.Common.Models.Ordbok.Api;
using Nellebot.Services;
using Nellebot.Services.Ordbok;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nellebot.Tests
{
    [TestClass]
    public class OrdbokTests
    {

        [TestMethod]
        public async Task TestDeserialization()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            var file = Path.Combine(directory, "test.json");

            var json = await File.ReadAllTextAsync(file);

            try
            {
                var result = JsonSerializer.Deserialize<OrdbokSearchResponse>(json);

            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }

        [TestMethod]
        public async Task TestModelMapping()
        {
            var directory = AppDomain.CurrentDomain.BaseDirectory;

            var file = Path.Combine(directory, "test.json");

            var json = await File.ReadAllTextAsync(file);

            var result = JsonSerializer.Deserialize<OrdbokSearchResponse>(json);

            foreach(var resultItem in result!)
            {
                try
                {
                    var localizationService = new Mock<ILocalizationService>();

                    localizationService
                        .Setup(m => m.GetString(It.IsAny<LocalizationResource>(), It.IsAny<string>()))
                        .Returns("what");

                    var modelMapper = new OrdbokModelMapper(localizationService.Object);

                    var article = modelMapper.MapArticle(resultItem);
                }
                catch (Exception ex)
                {
                    Assert.Fail(ex.ToString());
                }
            }

        }
    }
}
