using Microsoft.Extensions.Logging;
using Nellebot.Services.HtmlToImage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class HtmlToImageService
    {
        private readonly WkHtmlToImageClient _wkHtmlToImageClient;
        private readonly ILogger<HtmlToImageService> _logger;

        public HtmlToImageService(
            WkHtmlToImageClient wkHtmlToImageClient,
            ILogger<HtmlToImageService> logger
            )
        {
            _wkHtmlToImageClient = wkHtmlToImageClient;
            _logger = logger;
        }

        public async Task<GenerateImageFileResult> GenerateImageFile(string html)
        {
            var tempFolder = Path.GetTempPath();

            var fileName = Guid.NewGuid().ToString();

            var tempHtmlFilePath = Path.Combine(tempFolder, $"{fileName}.html");
            var tempImageFilePath = Path.Combine(tempFolder, $"{fileName}.png");

            await File.WriteAllTextAsync(tempHtmlFilePath, html);

            var args = new WkHtmlToImageArguments()
            {
                SourceHtmlFilePath = tempHtmlFilePath,
                DestinationImageFilePath = tempImageFilePath
            };

            await _wkHtmlToImageClient.GenerateImage(args);

            var imageFileStream = await ReadFileAsync(tempImageFilePath);
            var htmlFileStream = await ReadFileAsync(tempHtmlFilePath);

            return new GenerateImageFileResult
            {
                ImageFileName = $"{fileName}.png",
                ImageFileStream = imageFileStream,
                HtmlFileStream = htmlFileStream
            };
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

    public class GenerateImageFileResult
    {
        public string ImageFileName { get; set; } = string.Empty;
        public FileStream ImageFileStream = null!;
        public FileStream HtmlFileStream = null!;
    }
}
