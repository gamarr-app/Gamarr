using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    public class STJUtcConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // A JSON null for a non-nullable DateTime reaches the converter, and
            // Parse(null) threw ArgumentNullException: a 500 instead of a 400 (Sentry 4S).
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected a string for DateTime, got {reader.TokenType}");
            }

            try
            {
                return DateTime.Parse(reader.GetString(), CultureInfo.InvariantCulture).ToUniversalTime();
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new JsonException(ex.Message, ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ"));
        }
    }
}
