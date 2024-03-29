using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Nellebot.Data.Repositories;

namespace Nellebot.Services;

public enum LocalizationResource
{
    Ordbok,
}

public interface ILocalizationService
{
    string GetString(string key, LocalizationResource resource, string dictionaryName);
}

public class LocalizationService : ILocalizationService
{
    private readonly Lazy<ValueTask<Dictionary<string, string>>> _ordbokDictionaryNb;
    private readonly Lazy<ValueTask<Dictionary<string, string>>> _ordbokDictionaryNn;
    private readonly OrdbokRepository _ordbokRepo;

    public LocalizationService(OrdbokRepository ordbokRepo)
    {
        _ordbokDictionaryNb = new Lazy<ValueTask<Dictionary<string, string>>>(() => LoadDictionary("bm", "no_nb"));
        _ordbokDictionaryNn = new Lazy<ValueTask<Dictionary<string, string>>>(() => LoadDictionary("nn", "no_nn"));
        _ordbokRepo = ordbokRepo;
    }

    public string GetString(string key, LocalizationResource resource, string dictionaryName)
    {
        var lazyDictionary = GetResourceDictionary(resource, dictionaryName);

        // TODO rewrite to async
        var dictionary = lazyDictionary.Value.GetAwaiter().GetResult();

        if (dictionary.TryGetValue(key, out var value) && value != null)
        {
            return value;
        }

        // Mark elements that are missing from localization file
        return $"?{key}?";
    }

    private Lazy<ValueTask<Dictionary<string, string>>> GetResourceDictionary(
        LocalizationResource resource,
        string dictionaryName)
    {
        return resource switch
        {
            LocalizationResource.Ordbok => dictionaryName switch
            {
                "nb" => _ordbokDictionaryNb,
                "bm" => _ordbokDictionaryNb,
                "nn" => _ordbokDictionaryNn,
                _ => _ordbokDictionaryNb,
            },
            _ => throw new ArgumentException($"Unknown dictionary {resource}"),
        };
    }

    private async ValueTask<Dictionary<string, string>> LoadDictionary(string dictionaryName, string locale)
    {
        var concepts = await _ordbokRepo.GetConceptStore(dictionaryName)
                       ?? throw new Exception($"Could not load dictionary resource for {dictionaryName}");

        var result = concepts.Concepts;

        var fileContent = await File.ReadAllTextAsync($"Resources/Localization/Ordbok_{locale}.json", Encoding.UTF8);

        var serializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
        };

        var conceptsExtra = JsonSerializer.Deserialize<Dictionary<string, string>>(fileContent, serializerOptions)
                            ?? throw new Exception($"Could not load dictionary resource for {locale}");

        // append extra concepts to result dictionary if they are not already present
        foreach (var (key, value) in conceptsExtra)
        {
            if (!result.ContainsKey(key))
            {
                result.Add(key, value);
            }
        }

        return result;
    }
}
