using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nellebot.Services;
using Nellebot.Services.Glosbe;
using PuppeteerSharp;
using Scriban;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Tests
{
    [TestClass]
    [Ignore]
    public class GlosbeTests
    {
        [TestMethod]
        public async Task TestGlosbe()
        {
            try
            {
                var puppeteerFactory = new PuppeteerFactory();

                var glosbeClient = new GlosbeClient(puppeteerFactory);

                var translationResult = await glosbeClient.GetTranslation("nn", "en", "hus");

                var glosbeModelMapper = new GlosbeModelMapper();

                var model = glosbeModelMapper.MapTranslationResult(translationResult);

                var textTemplateSource = await File.ReadAllTextAsync($"TestFiles/TestGlosbeArticle.sbntxt");
                var textTemplate = Template.Parse(textTemplateSource);
                var textTemplateResult = textTemplate.Render(new { model.Article, model.QueryUrl});

                var trimmedResult = textTemplateResult.Trim();
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.ToString());
            }
        }
    }
}
