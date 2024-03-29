using System.IO;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Nellebot.Services.HtmlToImage;

public class HtmlToImageService
{
    private const int _imageWidthInPixels = 800;

    private readonly PuppeteerFactory _puppeteerFactory;

    public HtmlToImageService(
        PuppeteerFactory puppeteerFactory)
    {
        _puppeteerFactory = puppeteerFactory;
    }

    public async Task<GenerateImageFileResult> GenerateImageFile(string html)
    {
        var randomFilename = Path.GetRandomFileName();
        var randomFilenamePath = Path.Combine(Path.GetTempPath(), randomFilename);

        var tempHtmlFilePath = $"{randomFilenamePath}.html";
        var tempImageFilePath = $"{randomFilenamePath}.png";
        var randomImageFileName = $"{randomFilename}.png";

        await File.WriteAllTextAsync(tempHtmlFilePath, html);

        using var browser = await _puppeteerFactory.BuildBrowser();
        using var page = await browser.NewPageAsync();

        await page.GoToAsync($"file://{Path.GetFullPath(tempHtmlFilePath)}");
        await page.SetViewportAsync(new ViewPortOptions
        {
            Width = _imageWidthInPixels,
        });
        await page.ScreenshotAsync(tempImageFilePath);

        var imageFileStream = await ReadFileAsync(tempImageFilePath);
        var htmlFileStream = await ReadFileAsync(tempHtmlFilePath);

        return new GenerateImageFileResult(
                                           randomImageFileName,
                                           imageFileStream,
                                           htmlFileStream);
    }

    private async Task<FileStream> ReadFileAsync(string filePath)
    {
        var imageFileStream = File.OpenRead(filePath);
        var sr = new StreamReader(imageFileStream);
        await sr.ReadToEndAsync();

        await imageFileStream.FlushAsync();
        imageFileStream.Position = 0;

        return imageFileStream;
    }
}
