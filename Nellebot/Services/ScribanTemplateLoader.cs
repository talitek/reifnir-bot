using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Scriban;

namespace Nellebot.Services;

public enum ScribanTemplateType
{
    Text,
    Html,
}

public class ScribanTemplateLoader
{
    private readonly Dictionary<string, Template> _templateCache = new();

    public async Task<string> LoadTemplate(string templateName, ScribanTemplateType type)
    {
        var extension = type == ScribanTemplateType.Text ? "sbntxt" : "sbnhtml";

        var templateString = await File.ReadAllTextAsync($"Resources/ScribanTemplates/{templateName}.{extension}");

        return templateString;
    }

    public async Task<Template> LoadTemplateV2(string templateName, ScribanTemplateType type)
    {
        var extension = type == ScribanTemplateType.Text ? "sbntxt" : "sbnhtml";

        if (_templateCache.ContainsKey(templateName)) return _templateCache[templateName];

        var templateString = await File.ReadAllTextAsync($"Resources/ScribanTemplates/{templateName}.{extension}");

        var template = Template.Parse(templateString);

#if !DEBUG
        _templateCache.Add(templateName, template);
#endif

        return template;
    }
}
