using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Converters
{
    public class DefinitionElementConverter : JsonConverter<DefinitionElement>
    {
        public override DefinitionElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

            DefinitionElement? result;

            switch (typeDiscriminator.ToLower())
            {
                case "explanation":
                    result = JsonSerializer.Deserialize<Explanation>(ref reader, options);
                    break;
                case "example":
                    result = JsonSerializer.Deserialize<Example>(ref reader, options);
                    break;
                default:
                    // Read and throw away object so that the reader reaches EndObject token
                    _ = JsonSerializer.Deserialize<object>(ref reader, options);
                    result = null;
                    break;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, DefinitionElement value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case Explanation explanation:
                    JsonSerializer.Serialize(writer, explanation);
                    break;
                case Example example:
                    JsonSerializer.Serialize(writer, example);
                    break;
                default:
                    throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
            }
        }
    }
}
