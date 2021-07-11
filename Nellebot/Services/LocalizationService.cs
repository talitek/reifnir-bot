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
        string GetString(string key, LocalizationResource resource, LocalizationLocale? locale);
        string GetString(string key, LocalizationResource resource, string? locale);
    }

    public class LocalizationService : ILocalizationService
    {
        private Lazy<Dictionary<string, string>> _ordbokConceptsDictionaryNb;
        private Lazy<Dictionary<string, string>> _ordbokConceptsDictionaryNn;

        public LocalizationService()
        {
            _ordbokConceptsDictionaryNb = new Lazy<Dictionary<string, string>>(() => LoadDictionary("OrdbokConcepts_no_nb"));
            _ordbokConceptsDictionaryNn = new Lazy<Dictionary<string, string>>(() => LoadDictionary("OrdbokConcepts_no_nn"));
        }

        public string GetString(string key, LocalizationResource resource, LocalizationLocale? locale)
        {
            var dictionary = GetResourceDictionary(resource, locale);

            if (dictionary.Value == null)
                return key;

            if (dictionary.Value.ContainsKey(key))
                return dictionary.Value[key];

            // Temporarily mark elements that are missing from localization file
            return $"?{key}?";
        }

        public string GetString(string key, LocalizationResource resource, string? locale)
        {
            var localizationLocale = locale?.ToLower() switch
            {
                "bob" => LocalizationLocale.NoNb,
                "nob" => LocalizationLocale.NoNn,
                _ => LocalizationLocale.NoNb
            };

            return GetString(key, resource, localizationLocale);
        }

        private Lazy<Dictionary<string, string>> GetResourceDictionary(LocalizationResource resource, LocalizationLocale? locale)
        {
            return resource switch
            {
                LocalizationResource.OrdbokConcepts => locale switch 
                {
                    LocalizationLocale.NoNb => _ordbokConceptsDictionaryNb,
                    LocalizationLocale.NoNn => _ordbokConceptsDictionaryNn,
                    _ => _ordbokConceptsDictionaryNb
                },
                _ => throw new ArgumentException($"Unknown dictionary {resource}")
            };
        }

        private Dictionary<string, string> LoadDictionary(string filename)
        {
            // TODO double check if Latin1 works on linux
            var fileContent = File.ReadAllText($"Resources/Localization/{filename}.json", Encoding.Latin1);

            var serializerOptions = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true
            };

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(fileContent, serializerOptions);

            if (dictionary == null)
                throw new ArgumentException($"Could not load dictionary resource for {filename}");

            return dictionary;
        }
    }

    public enum LocalizationResource
    {
        OrdbokConcepts
    }

    public enum LocalizationLocale
    {
        NoNb,
        NoNn
    }
}
