using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Converters
{
    public class EtymologyLanguageElementConverter : JsonConverter<EtymologyLanguageElement>
    {
        public override EtymologyLanguageElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

            EtymologyLanguageElement? result;

            switch (typeDiscriminator.ToLower())
            {
                case "language":
                    result = JsonSerializer.Deserialize<EtymologyLanguageLanguage>(ref reader, options);
                    break;
                case "relation":
                    result = JsonSerializer.Deserialize<EtymologyLanguageRelation>(ref reader, options);
                    break;
                case "usage":
                    result = JsonSerializer.Deserialize<EtymologyLanguageUsage>(ref reader, options);
                    break;
                default:
                    // Read and throw away object so that the reader reaches EndObject token
                    _ = JsonSerializer.Deserialize<object>(ref reader, options);
                    result = null;
                    break;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, EtymologyLanguageElement value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case EtymologyLanguageLanguage language:
                    JsonSerializer.Serialize(writer, language);
                    break;
                case EtymologyLanguageRelation relation:
                    JsonSerializer.Serialize(writer, relation);
                    break;
                case EtymologyLanguageUsage usage:
                    JsonSerializer.Serialize(writer, usage);
                    break;
                default:
                    throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
            }
        }
    }
}
