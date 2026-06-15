using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoslynRules.EntityFrameworkCore.Entities;

/// <summary>
/// EF Core entity for Rule. NOT sealed — supports lazy loading proxies.
/// Maps to the same database schema as the domain Rule model.
/// Use RuleEntity.ToDomainModel() to get the immutable sealed Rule.
/// </summary>
public class RuleEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public string? DescriptionKey { get; set; }
    public string Expression { get; set; } = string.Empty;
    public string? Action { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 50;
    public TimeSpan? CacheDuration { get; set; }
    public TimeSpan? Timeout { get; set; }
    public Guid? DependsOnRuleId { get; set; }
    public Guid? ParentRuleId { get; set; }
    public Guid? WorkflowId { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Semantic version of this rule (e.g. "1.2.3").
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    // Navigation properties — virtual for lazy loading
    public virtual RuleEntity? ParentRule { get; set; }
    public virtual ICollection<RuleEntity> ChildRules { get; set; } = new List<RuleEntity>();
    public virtual WorkflowEntity? Workflow { get; set; }

    /// <summary>
    /// Converts this EF entity to the sealed domain model.
    /// Recursively converts child rules.
    /// </summary>
    public Models.Rule ToDomainModel()
    {
        var rule = new Models.Rule(Id)
        {
            Description = Description,
            DescriptionKey = DescriptionKey,
            Expression = Expression,
            Action = Action ?? string.Empty,
            IsActive = IsActive,
            Priority = Priority,
            CacheDuration = CacheDuration,
            Timeout = Timeout,
            DependsOnRuleId = DependsOnRuleId,
            ParentRuleId = ParentRuleId,
            WorkflowId = WorkflowId,
            ModifiedBy = ModifiedBy,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Models.RuleVersion.Parse(Version)
        };

        foreach (var child in ChildRules)
        {
            rule.ChildRules.Add(child.ToDomainModel());
        }

        return rule;
    }
}
