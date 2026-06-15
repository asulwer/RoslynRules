using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Snapshots;

/// <summary>
/// Immutable snapshot of a Workflow that can be serialized and deserialized.
/// Captures all workflow metadata and rule definitions but not compiled delegates.
/// Used for persisting workflow definitions and loading them in AOT environments.
/// </summary>
public sealed class WorkflowSnapshot
{
    /// <summary>Unique identifier for the workflow.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Human-readable description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Semantic version of this workflow.</summary>
    public RuleVersion Version { get; init; } = new(1, 0, 0);

    /// <summary>When this workflow was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When this workflow was last modified.</summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Optional modifier identifier.</summary>
    public string? ModifiedBy { get; init; }

    /// <summary>When false, the entire workflow and its rules are skipped.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Top-level rule snapshots in this workflow.</summary>
    public IReadOnlyList<RuleSnapshot> Rules { get; init; } = Array.Empty<RuleSnapshot>();

    /// <summary>
    /// Creates a snapshot from a live Workflow instance.
    /// JIT-only: requires reflection access to Workflow properties.
    /// </summary>
    public static WorkflowSnapshot FromWorkflow(Workflow workflow)
    {
        return new WorkflowSnapshot
        {
            Id = workflow.Id,
            Description = workflow.Description,
            Version = workflow.Version,
            CreatedAt = workflow.CreatedAt,
            ModifiedAt = workflow.ModifiedAt,
            ModifiedBy = workflow.ModifiedBy,
            IsActive = workflow.IsActive,
            Rules = workflow.Rules.Select(RuleSnapshot.FromRule).ToList()
        };
    }

    /// <summary>
    /// Reconstructs a live Workflow from this snapshot.
    /// AOT-safe: no reflection required.
    /// The returned Workflow is not compiled; call Compile() before execution.
    /// </summary>
    public Workflow ToWorkflow()
    {
        var workflow = new Workflow
        {
            Id = Id,
            Description = Description,
            Version = Version,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            ModifiedBy = ModifiedBy,
            IsActive = IsActive
        };

        foreach (var ruleSnapshot in Rules)
        {
            workflow.Rules.Add(ruleSnapshot.ToRule());
        }

        return workflow;
    }
}
