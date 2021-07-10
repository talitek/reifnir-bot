using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Converters
{
    public class EtymologyGroupConverter : JsonConverter<EtymologyGroup>
    {
        public override EtymologyGroup? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

            EtymologyGroup? result;

            switch (typeDiscriminator.ToLower())
            {
                case "etymology_language":
                    result = JsonSerializer.Deserialize<EtymologyLanguage>(ref reader, options);
                    break;
                case "etymology_reference":
                    result = JsonSerializer.Deserialize<EtymologyReference>(ref reader, options);
                    break;
                default:
                    // Read and throw away object so that the reader reaches EndObject token
                    _ = JsonSerializer.Deserialize<object>(ref reader, options);
                    result = null;
                    break;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, EtymologyGroup value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case EtymologyLanguage language:
                    JsonSerializer.Serialize(writer, language);
                    break;
                case EtymologyReference reference:
                    JsonSerializer.Serialize(writer, reference);
                    break;
                default:
                    throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
            }
        }
    }
}
