using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nocturne.Core.Models.Serializers;

/// <summary>
/// JSON converter that handles flexible long (Int64) serialization for Nightscout compatibility.
/// Nightscout may send numeric values as either numbers or strings depending on the context.
/// This converter handles both cases gracefully.
/// </summary>
public class FlexibleLongConverter : JsonConverter<long>
{
    public override long Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64();

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return 0;

                if (long.TryParse(stringValue, out var result))
                    return result;

                // Try parsing as double and truncating (for values like "1234567890.0")
                if (double.TryParse(stringValue, out var doubleResult))
                    return (long)doubleResult;

                return 0;

            case JsonTokenType.Null:
                return 0;

            default:
                return 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// JSON converter that handles flexible nullable long (Int64?) serialization for Nightscout compatibility.
/// Nightscout may send numeric values as either numbers or strings depending on the context.
/// This converter handles both cases gracefully.
/// </summary>
public class FlexibleNullableLongConverter : JsonConverter<long?>
{
    public override long? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                return reader.GetInt64();

            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;

                if (long.TryParse(stringValue, out var result))
                    return result;

                // Try parsing as double and truncating (for values like "1234567890.0")
                if (double.TryParse(stringValue, out var doubleResult))
                    return (long)doubleResult;

                return null;

            case JsonTokenType.Null:
                return null;

            default:
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
