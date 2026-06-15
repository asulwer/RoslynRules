using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Json;

/// <summary>
/// Custom JSON converter for TimeSpan that serializes as total seconds
/// for human-readable JSON output.
/// </summary>
internal sealed class TimeSpanJsonConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.Number)
        {
            var seconds = reader.GetDouble();
            return TimeSpan.FromSeconds(seconds);
        }

        throw new JsonException($"Expected number or null for TimeSpan, got {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value.TotalSeconds);
        else
            writer.WriteNullValue();
    }
}
