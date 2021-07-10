using System.Text.Json;

namespace Nellebot.Common.Models.Ordbok.Converters
{
    public static class TypeElementConverterHelper
    {
        public static readonly string TypePropertyName = "type_";

        public static string GetTypeDiscriminator(ref Utf8JsonReader reader)
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

            return typeDiscriminator;
        }
    }
}
