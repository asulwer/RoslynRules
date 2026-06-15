using RoslynRules.Snapshots;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace RoslynRules.Json;

/// <summary>
/// JSON serializer for workflow and rule snapshots.
/// Uses System.Text.Json source generators for AOT/trimming compatibility.
/// 
/// JIT: Can serialize (create snapshots) and deserialize.
/// AOT: Can only deserialize pre-existing snapshots.
/// </summary>
public sealed class JsonSnapshotSerializer : ISnapshotSerializer
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Creates a JsonSnapshotSerializer with the default AOT-safe options.
    /// </summary>
    public JsonSnapshotSerializer()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = RoslynRulesJsonContext.Default,
            Converters =
            {
                new JsonStringEnumConverter(),
                new TimeSpanJsonConverter(),
                new RuleVersionJsonConverter()
            }
        };
    }

    /// <summary>
    /// Creates a JsonSnapshotSerializer with custom options.
    /// </summary>
    public JsonSnapshotSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string Serialize(WorkflowSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, _options);

    /// <inheritdoc />
    public WorkflowSnapshot DeserializeWorkflow(string data)
    {
        var result = JsonSerializer.Deserialize<WorkflowSnapshot>(data, _options);
        return result ?? throw new JsonException("Failed to deserialize workflow snapshot from JSON.");
    }

    /// <inheritdoc />
    public string Serialize(RuleSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, _options);

    /// <inheritdoc />
    public RuleSnapshot DeserializeRule(string data)
    {
        var result = JsonSerializer.Deserialize<RuleSnapshot>(data, _options);
        return result ?? throw new JsonException("Failed to deserialize rule snapshot from JSON.");
    }
}
