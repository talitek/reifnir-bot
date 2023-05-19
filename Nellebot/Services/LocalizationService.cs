using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nellebot.Data.Repositories;

namespace Nellebot.Services;

public enum LocalizationResource
{
    OrdbokConcepts,
}

public interface ILocalizationService
{
    string GetString(string key, LocalizationResource resource, string locale);
}

public class LocalizationService : ILocalizationService
{
    private readonly Lazy<ValueTask<Dictionary<string, string>>> _ordbokConceptsDictionaryNb;
    private readonly Lazy<ValueTask<Dictionary<string, string>>> _ordbokConceptsDictionaryNn;
    private readonly OrdbokRepository _ordbokRepo;

    public LocalizationService(OrdbokRepository ordbokRepo)
    {
        _ordbokConceptsDictionaryNb = new Lazy<ValueTask<Dictionary<string, string>>>(() => LoadDictionary("bm"));
        _ordbokConceptsDictionaryNn = new Lazy<ValueTask<Dictionary<string, string>>>(() => LoadDictionary("nn"));
        _ordbokRepo = ordbokRepo;
    }

    public string GetString(string key, LocalizationResource resource, string locale)
    {
        var lazyDictionary = GetResourceDictionary(resource, locale);

        // TODO rewrite to async
        var dictionary = lazyDictionary.Value.GetAwaiter().GetResult();

        if (dictionary.TryGetValue(key, out string? value) && value != null)
            return value;

        // Mark elements that are missing from localization file
        return $"?{key}?";
    }

    private Lazy<ValueTask<Dictionary<string, string>>> GetResourceDictionary(LocalizationResource resource, string locale)
    {
        return resource switch
        {
            LocalizationResource.OrdbokConcepts => locale switch
            {
                "nb" => _ordbokConceptsDictionaryNb,
                "bm" => _ordbokConceptsDictionaryNb,
                "nn" => _ordbokConceptsDictionaryNn,
                _ => _ordbokConceptsDictionaryNb,
            },
            _ => throw new ArgumentException($"Unknown dictionary {resource}"),
        };
    }

    private async ValueTask<Dictionary<string, string>> LoadDictionary(string locale)
    {
        var concepts = (await _ordbokRepo.GetConceptStore(locale))
            ?? throw new ArgumentException($"Could not load dictionary resource for {locale}");

        return concepts.Concepts;
    }
}
