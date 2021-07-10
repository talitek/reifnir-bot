using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Converters
{
    public class EtymologyLittElementConverter : JsonConverter<EtymologyLittElement>
    {
        public override EtymologyLittElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

            EtymologyLittElement? result;

            switch (typeDiscriminator.ToLower())
            {
                case "usage":
                    result = JsonSerializer.Deserialize<EtymologyLittUsage>(ref reader, options);
                    break;
                default:
                    // Read and throw away object so that the reader reaches EndObject token
                    _ = JsonSerializer.Deserialize<object>(ref reader, options);
                    result = null;
                    break;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, EtymologyLittElement value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case EtymologyLittUsage usage:
                    JsonSerializer.Serialize(writer, usage);
                    break;
                default:
                    throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
            }
        }
    }
}
