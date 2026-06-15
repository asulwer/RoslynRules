using RoslynRules.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Json;

/// <summary>
/// JSON converter for <see cref="RuleVersion"> values.
/// Serializes to SemVer 2.0.0 string format.
/// </summary>
public sealed class RuleVersionJsonConverter : JsonConverter<RuleVersion>
{
    public override RuleVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return RuleVersion.Unspecified;

        return RuleVersion.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, RuleVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
