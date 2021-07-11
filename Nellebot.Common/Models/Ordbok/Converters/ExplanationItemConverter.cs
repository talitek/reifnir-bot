using Nellebot.Common.Models.Ordbok.Api;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nellebot.Common.Models.Ordbok.Converters
{
    public class ExplanationItemConverter : JsonConverter<ExplanationItem>
    {
        public override ExplanationItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeDiscriminator = TypeElementConverterHelper.GetTypeDiscriminator(ref reader);

            ExplanationItem? result;

            switch (typeDiscriminator.ToLower())
            {
                case "relation":
                case "domain":
                case "entity":
                case "modifier":
                case "grammar":
                case "rhetoric":
                    result = JsonSerializer.Deserialize<ExplanationIdElement>(ref reader, options);
                    break;
                case "usage":
                    result = JsonSerializer.Deserialize<ExplanationTextElement>(ref reader, options);
                    break;
                case "article_ref":
                    result = JsonSerializer.Deserialize<ExplanationItemArticleRef>(ref reader, options);
                    break;
                default:
                    // Read and throw away object so that the reader reaches EndObject token
                    _ = JsonSerializer.Deserialize<object>(ref reader, options);
                    result = null;
                    break;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, ExplanationItem value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case ExplanationIdElement idElement:
                    JsonSerializer.Serialize(writer, idElement);
                    break;
                case ExplanationTextElement textElement:
                    JsonSerializer.Serialize(writer, textElement);
                    break;
                case ExplanationItemArticleRef articleRef:
                    JsonSerializer.Serialize(writer, articleRef);
                    break;
                default:
                    throw new JsonException($"Unknown subclass of {value.GetType().FullName}");
            }
        }
    }
}
