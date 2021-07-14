namespace Nellebot.Services.HtmlToImage
{
    public class WkHtmlToImageArguments
    {
        public string SourceHtmlFilePath { get; set; } = string.Empty;
        public string DestinationImageFilePath { get; set; } = string.Empty;
        public string ImageFormat { get; set; } = "png";
        public uint ImageQuality { get; set; } = 80;
        public uint ImageWidthInPixels { get; set; } = 800;
        public string LogLevel { get; set; } = "none";
    }
}
