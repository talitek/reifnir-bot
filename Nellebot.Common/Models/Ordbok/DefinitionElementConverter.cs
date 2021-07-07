using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Nellebot.Common.Models.Ordbok
{
    public class DefinitionElementConverter : JsonConverter<DefinitionElement>
    {
        private static readonly string TypePropertyName = "type_";

        public override DefinitionElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Utf8JsonReader readerClone = reader;

            if (readerClone.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = readerClone.GetString();
            if (propertyName != TypePropertyName)
            {
                throw new JsonException();
            }

            readerClone.Read();
            if (readerClone.TokenType != JsonTokenType.String)
            {
                throw new JsonException();
            }

            var typeDiscriminator = readerClone.GetString();
            if (typeDiscriminator == null)
            {
                throw new JsonException("Missing typeDiscriminator");
            }

            if (typeDiscriminator != "explanation" && typeDiscriminator != "example")
            {
                var x = 1;
            }

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
            if (value is Explanation explanation)
            {
                JsonSerializer.Serialize(writer, explanation);
            }
            else if (value is Example example)
            {
                JsonSerializer.Serialize(writer, example);
            }
            else
            {
                throw new JsonException($"Unknown subclass of {typeof(DefinitionElement).FullName}");
            }
        }
    }
}
