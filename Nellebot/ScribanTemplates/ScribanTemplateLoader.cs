using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nellebot.ScribanTemplates
{
    public class ScribanTemplateLoader
    {
        public async Task<string> LoadTemplate(string templateName)
        {
            var templateString = await File.ReadAllTextAsync($"ScribanTemplates/{templateName}.sbn");

            return templateString;
        }
    }
}
