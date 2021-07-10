using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Nellebot.Services
{
    public interface ILocalizationService
    {
        string GetString(LocalizationResource resource, string key);
    }

    public class LocalizationService : ILocalizationService
    {
        private Lazy<Dictionary<string, string>> _ordbokDictionary;

        public LocalizationService()
        {
            _ordbokDictionary = new Lazy<Dictionary<string, string>>(() => LoadDictionary("Ordbok"));
        }

        public string GetString(LocalizationResource resource, string key)
        {
            var dictionary = GetResourceDictionary(resource);

            if (dictionary.Value == null)
                return key;

            if (dictionary.Value.ContainsKey(key))
                return dictionary.Value[key];

            // Temporarily mark elements that are missing from localization file
            return $"?{key}?";
        }

        private Lazy<Dictionary<string, string>> GetResourceDictionary(LocalizationResource resource)
        {
            return resource switch
            {
                LocalizationResource.Ordbok => _ordbokDictionary,
                _ => throw new ArgumentException($"Unknown dictionary {resource}")
            };
        }

        private Dictionary<string, string> LoadDictionary(string filename)
        {
            // TODO double check if Latin1 works on linux
            var fileContent = File.ReadAllText($"Resources/Localization/{filename}.json", Encoding.Latin1);

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(fileContent);

            if (dictionary == null)
                throw new ArgumentException($"Could not load dictionary resource for {filename}");

            return dictionary;
        }
    }

    public enum LocalizationResource
    {
        Ordbok
    }
}
