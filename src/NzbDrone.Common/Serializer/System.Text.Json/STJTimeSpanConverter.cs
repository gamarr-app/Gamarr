using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    public class STJTimeSpanConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // A JSON null for a non-nullable TimeSpan reaches the converter, and
            // Parse(null) threw ArgumentNullException: a 500 instead of a 400 (Sentry 4S).
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected a string for TimeSpan, got {reader.TokenType}");
            }

            try
            {
                return TimeSpan.Parse(reader.GetString(), CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new JsonException(ex.Message, ex);
            }
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
