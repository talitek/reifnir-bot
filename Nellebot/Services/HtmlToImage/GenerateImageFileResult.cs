using System.IO;

namespace Nellebot.Services;

public record GenerateImageFileResult(string ImageFileName, FileStream ImageFileStream, FileStream HtmlFileStream);
