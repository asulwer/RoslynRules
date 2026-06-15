using System;
using System.IO;

namespace RoslynRules.Snapshots;

/// <summary>
/// Interface for serializers that can save and load workflow snapshots.
/// Implementations provide format-specific serialization (JSON, XML, etc.).
/// AOT-safe implementations must not use reflection.
/// </summary>
public interface ISnapshotSerializer
{
    /// <summary>
    /// Serializes a workflow snapshot to a string.
    /// </summary>
    string Serialize(WorkflowSnapshot snapshot);

    /// <summary>
    /// Deserializes a workflow snapshot from a string.
    /// </summary>
    WorkflowSnapshot DeserializeWorkflow(string data);

    /// <summary>
    /// Serializes a rule snapshot to a string.
    /// </summary>
    string Serialize(RuleSnapshot snapshot);

    /// <summary>
    /// Deserializes a rule snapshot from a string.
    /// </summary>
    RuleSnapshot DeserializeRule(string data);
}

/// <summary>
/// Extension methods for ISnapshotSerializer providing file I/O convenience.
/// </summary>
public static class SnapshotSerializerExtensions
{
    /// <summary>
    /// Saves a workflow snapshot to a file.
    /// </summary>
    public static void SaveWorkflowToFile(this ISnapshotSerializer serializer, WorkflowSnapshot snapshot, string filePath)
        => File.WriteAllText(filePath, serializer.Serialize(snapshot));

    /// <summary>
    /// Loads a workflow snapshot from a file.
    /// </summary>
    public static WorkflowSnapshot LoadWorkflowFromFile(this ISnapshotSerializer serializer, string filePath)
        => serializer.DeserializeWorkflow(File.ReadAllText(filePath));

    /// <summary>
    /// Saves a rule snapshot to a file.
    /// </summary>
    public static void SaveRuleToFile(this ISnapshotSerializer serializer, RuleSnapshot snapshot, string filePath)
        => File.WriteAllText(filePath, serializer.Serialize(snapshot));

    /// <summary>
    /// Loads a rule snapshot from a file.
    /// </summary>
    public static RuleSnapshot LoadRuleFromFile(this ISnapshotSerializer serializer, string filePath)
        => serializer.DeserializeRule(File.ReadAllText(filePath));
}
