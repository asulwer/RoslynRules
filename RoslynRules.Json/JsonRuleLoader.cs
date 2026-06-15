using RoslynRules.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RoslynRules.Json;

/// <summary>
/// JSON serialization and deserialization for Rules and Workflows.
/// Uses System.Text.Json with custom converters for trim/AOT-safe
/// serialization without reflection-based ID restoration.
/// 
/// For AOT/trimming scenarios, use <see cref="JsonRuleLoader.AotOptions"> which use the
/// source-generated serializer context for better compatibility.
/// </summary>
public static class JsonRuleLoader
{
    /// <summary>
    /// Default JSON options with camelCase naming, indented output,
    /// and custom converters for TimeSpan serialization.
    /// Uses reflection-based serialization (not trim/AOT safe).
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new TimeSpanJsonConverter(),
            new RuleVersionJsonConverter()
        }
    };

    /// <summary>
    /// JSON options optimized for AOT/trimming scenarios.
    /// Uses the source-generated <see cref="RoslynRulesJsonContext"> for serialization.
    /// Falls back to reflection-based if the context doesn't support a type.
    /// </summary>
    public static JsonSerializerOptions AotOptions { get; } = new JsonSerializerOptions
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

    /// <summary>
    /// Serializes a workflow to JSON string.
    /// Uses reflection-based serializer by default. For AOT, use <see cref="SerializeAot(Workflow, JsonSerializerOptions?)"/>.
    /// </summary>
    public static string Serialize(Workflow workflow, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(workflow, options ?? DefaultOptions);

    /// <summary>
    /// Serializes a rule to JSON string.
    /// Uses reflection-based serializer by default. For AOT, use <see cref="SerializeAot(Rule, JsonSerializerOptions?)"/>.
    /// </summary>
    public static string Serialize(Rule rule, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(rule, options ?? DefaultOptions);

    /// <summary>
    /// AOT-safe serialization for workflows using source-generated JSON context.
    /// </summary>
    public static string SerializeAot(Workflow workflow, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(workflow, options ?? AotOptions);

    /// <summary>
    /// AOT-safe serialization for rules using source-generated JSON context.
    /// </summary>
    public static string SerializeAot(Rule rule, JsonSerializerOptions? options = null)
        => JsonSerializer.Serialize(rule, options ?? AotOptions);

    /// <summary>
    /// Deserializes a workflow from JSON string.
    /// Uses reflection-based serializer by default. For AOT, use <see cref="DeserializeWorkflowAot(string, JsonSerializerOptions?)"/>.
    /// </summary>
    public static Workflow DeserializeWorkflow(string json, JsonSerializerOptions? options = null)
    {
        var workflow = JsonSerializer.Deserialize<Workflow>(json, options ?? DefaultOptions);
        return workflow ?? throw new JsonException("Failed to deserialize workflow from JSON.");
    }

    /// <summary>
    /// Deserializes a rule from JSON string.
    /// Uses reflection-based serializer by default. For AOT, use <see cref="DeserializeRuleAot(string, JsonSerializerOptions?)"/>.
    /// </summary>
    public static Rule DeserializeRule(string json, JsonSerializerOptions? options = null)
    {
        var rule = JsonSerializer.Deserialize<Rule>(json, options ?? DefaultOptions);
        return rule ?? throw new JsonException("Failed to deserialize rule from JSON.");
    }

    /// <summary>
    /// AOT-safe deserialization for workflows using source-generated JSON context.
    /// </summary>
    public static Workflow DeserializeWorkflowAot(string json, JsonSerializerOptions? options = null)
    {
        var workflow = JsonSerializer.Deserialize(json, typeof(Workflow), options ?? AotOptions) as Workflow;
        return workflow ?? throw new JsonException("Failed to deserialize workflow from JSON.");
    }

    /// <summary>
    /// AOT-safe deserialization for rules using source-generated JSON context.
    /// </summary>
    public static Rule DeserializeRuleAot(string json, JsonSerializerOptions? options = null)
    {
        var rule = JsonSerializer.Deserialize(json, typeof(Rule), options ?? AotOptions) as Rule;
        return rule ?? throw new JsonException("Failed to deserialize rule from JSON.");
    }

    /// <summary>
    /// Loads a workflow from a JSON file. Optionally validates the JSON schema before deserialization.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="options">Optional JSON serializer settings.</param>
    /// <param name="useAot">Use AOT-safe serializer.</param>
    /// <param name="validateSchema">When true, validates JSON structure before deserialization and throws if invalid.</param>
    public static Workflow LoadWorkflowFromFile(string filePath, JsonSerializerOptions? options = null, bool useAot = false, bool validateSchema = false)
    {
        var json = File.ReadAllText(filePath);
        if (validateSchema)
        {
            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            if (errors.Count > 0)
                throw new InvalidOperationException($"JSON schema validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
        return useAot
            ? DeserializeWorkflowAot(json, options)
            : DeserializeWorkflow(json, options);
    }

    /// <summary>
    /// Loads a rule from a JSON file. Optionally validates the JSON schema before deserialization.
    /// </summary>
    /// <param name="filePath">Path to the JSON file.</param>
    /// <param name="options">Optional JSON serializer settings.</param>
    /// <param name="useAot">Use AOT-safe serializer.</param>
    /// <param name="validateSchema">When true, validates JSON structure before deserialization and throws if invalid.</param>
    public static Rule LoadRuleFromFile(string filePath, JsonSerializerOptions? options = null, bool useAot = false, bool validateSchema = false)
    {
        var json = File.ReadAllText(filePath);
        if (validateSchema)
        {
            var errors = JsonSchemaValidator.ValidateRule(json);
            if (errors.Count > 0)
                throw new InvalidOperationException($"JSON schema validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
        return useAot
            ? DeserializeRuleAot(json, options)
            : DeserializeRule(json, options);
    }

    /// <summary>
    /// Saves a workflow to a JSON file.
    /// </summary>
    public static void SaveWorkflowToFile(Workflow workflow, string filePath, JsonSerializerOptions? options = null, bool useAot = false)
        => File.WriteAllText(filePath, useAot ? SerializeAot(workflow, options) : Serialize(workflow, options));

    /// <summary>
    /// Saves a rule to a JSON file.
    /// </summary>
    public static void SaveRuleToFile(Rule rule, string filePath, JsonSerializerOptions? options = null, bool useAot = false)
        => File.WriteAllText(filePath, useAot ? SerializeAot(rule, options) : Serialize(rule, options));
}
