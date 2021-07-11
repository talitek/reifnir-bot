using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public class HtmlToImageService
    {
        public async Task<string> GenerateImageFile(string html)
        {
            var tempFolder = Path.GetTempPath();

            var fileName = Guid.NewGuid().ToString();

            var tempHtmlFilePath = Path.Combine(tempFolder, $"{fileName}.html");
            var tempImageFilePath = Path.Combine(tempFolder, $"{fileName}.png");

            await File.WriteAllTextAsync(tempHtmlFilePath, html);

            await GenerateImage(tempHtmlFilePath, tempImageFilePath);

            return tempImageFilePath;
        }

        private async Task GenerateImage(string htmlFilePath, string imageFilePath)
        {
            var process = new Process();

            process.StartInfo.FileName = "wkhtmltoimage";
            process.StartInfo.ArgumentList.Add("-f");
            process.StartInfo.ArgumentList.Add("png");
            process.StartInfo.ArgumentList.Add(htmlFilePath);
            process.StartInfo.ArgumentList.Add(imageFilePath);
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();
            await process.WaitForExitAsync();
        }
    }
}
