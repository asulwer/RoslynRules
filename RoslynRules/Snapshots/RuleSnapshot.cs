using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Snapshots;

/// <summary>
/// Immutable snapshot of a Rule that can be serialized and deserialized.
/// Captures all rule metadata and expression strings but not compiled delegates.
/// Used for persisting rule definitions and loading them in AOT environments.
/// </summary>
public sealed class RuleSnapshot
{
    /// <summary>Unique identifier for the rule.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Semantic version of this rule.</summary>
    public RuleVersion Version { get; init; } = new(1, 0, 0);

    /// <summary>When this rule was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>When this rule was last modified.</summary>
    public DateTime ModifiedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Optional modifier identifier.</summary>
    public string? ModifiedBy { get; init; }

    /// <summary>Human-readable description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Localization key for description.</summary>
    public string? DescriptionKey { get; init; }

    /// <summary>When false, the rule is skipped during execution.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Execution priority. Higher values execute first.</summary>
    public int Priority { get; init; } = 0;

    /// <summary>C# boolean expression evaluated during execution.</summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>C# expression executed as an action when the rule succeeds.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Foreign key referencing the parent workflow.</summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>Foreign key referencing the parent rule.</summary>
    public Guid? ParentRuleId { get; init; }

    /// <summary>Maximum time allowed for rule execution.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Duration to cache rule evaluation results.</summary>
    public TimeSpan? CacheDuration { get; init; }

    /// <summary>Foreign key referencing another rule that this rule depends on.</summary>
    public Guid? DependsOnRuleId { get; init; }

    /// <summary>Child rule snapshots.</summary>
    public IReadOnlyList<RuleSnapshot> ChildRules { get; init; } = Array.Empty<RuleSnapshot>();

    /// <summary>
    /// Creates a snapshot from a live Rule instance.
    /// JIT-only: requires reflection access to Rule properties.
    /// </summary>
    public static RuleSnapshot FromRule(Rule rule)
    {
        return new RuleSnapshot
        {
            Id = rule.Id,
            Version = rule.Version,
            CreatedAt = rule.CreatedAt,
            ModifiedAt = rule.ModifiedAt,
            ModifiedBy = rule.ModifiedBy,
            Description = rule.Description,
            DescriptionKey = rule.DescriptionKey,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            Expression = rule.Expression,
            Action = rule.Action,
            WorkflowId = rule.WorkflowId,
            ParentRuleId = rule.ParentRuleId,
            Timeout = rule.Timeout,
            CacheDuration = rule.CacheDuration,
            DependsOnRuleId = rule.DependsOnRuleId,
            ChildRules = rule.ChildRules.Select(FromRule).ToList()
        };
    }

    /// <summary>
    /// Reconstructs a live Rule from this snapshot.
    /// AOT-safe: no reflection required.
    /// The returned Rule is not compiled; call Compile() on the parent Workflow to compile it.
    /// </summary>
    public Rule ToRule()
    {
        var rule = new Rule(Id)
        {
            Version = Version,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            ModifiedBy = ModifiedBy,
            Description = Description,
            DescriptionKey = DescriptionKey,
            IsActive = IsActive,
            Priority = Priority,
            Expression = Expression,
            Action = Action,
            WorkflowId = WorkflowId,
            ParentRuleId = ParentRuleId,
            Timeout = Timeout,
            CacheDuration = CacheDuration,
            DependsOnRuleId = DependsOnRuleId
        };

        foreach (var child in ChildRules)
        {
            rule.ChildRules.Add(child.ToRule());
        }

        return rule;
    }
}
