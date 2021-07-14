using System.Diagnostics;
using System.Threading.Tasks;

namespace Nellebot.Services.HtmlToImage
{
    public class WkHtmlToImageClient
    {
        public async Task GenerateImage(WkHtmlToImageArguments args)
        {
            var process = new Process();

            process.StartInfo.FileName = "wkhtmltoimage";

            process.StartInfo.ArgumentList.Add("--format");
            process.StartInfo.ArgumentList.Add(args.ImageFormat);

            process.StartInfo.ArgumentList.Add("--quality");
            process.StartInfo.ArgumentList.Add(args.ImageQuality.ToString());

            process.StartInfo.ArgumentList.Add("--width");
            process.StartInfo.ArgumentList.Add(args.ImageWidthInPixels.ToString());

            process.StartInfo.ArgumentList.Add("--log-level");
            process.StartInfo.ArgumentList.Add(args.LogLevel);

            process.StartInfo.ArgumentList.Add(args.SourceHtmlFilePath);
            process.StartInfo.ArgumentList.Add(args.DestinationImageFilePath);

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            await process.WaitForExitAsync();
        }
    }
}
