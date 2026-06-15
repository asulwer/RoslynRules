using RoslynRules.Models;
using RoslynRules.Snapshots;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Json;

/// <summary>
/// Source-generated JSON serializer context for trim/AOT-safe serialization.
/// Includes the core models and snapshot types needed for JSON round-tripping.
/// 
/// For full AOT support, reference this context when serializing:
/// <code>
/// JsonSerializer.Serialize(workflow, RoslynRulesJsonContext.Default.Workflow);
/// </code>
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
// Core models
[JsonSerializable(typeof(Workflow))]
[JsonSerializable(typeof(Rule))]
[JsonSerializable(typeof(RuleResult))]
[JsonSerializable(typeof(RuleParameter))]
[JsonSerializable(typeof(ValidationError))]
[JsonSerializable(typeof(RuleVersion))]
[JsonSerializable(typeof(RuleMetrics))]
// Snapshot types
[JsonSerializable(typeof(WorkflowSnapshot))]
[JsonSerializable(typeof(RuleSnapshot))]
[JsonSerializable(typeof(CompiledWorkflow))]
// Collection types
[JsonSerializable(typeof(List<Rule>))]
[JsonSerializable(typeof(List<RuleResult>))]
[JsonSerializable(typeof(List<RuleSnapshot>))]
[JsonSerializable(typeof(Dictionary<Guid, RuleVersion>))]
// Primitive types
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(Guid))]
public partial class RoslynRulesJsonContext : JsonSerializerContext
{
}
