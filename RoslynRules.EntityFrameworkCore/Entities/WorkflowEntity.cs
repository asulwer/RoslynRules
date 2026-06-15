using System.ComponentModel.DataAnnotations;

namespace RoslynRules.EntityFrameworkCore.Entities;

/// <summary>
/// EF Core entity for Workflow. NOT sealed — supports lazy loading proxies.
/// Maps to the same database schema as the domain Workflow model.
/// Use WorkflowEntity.ToDomainModel() to get the immutable Workflow with sealed Rules.
/// </summary>
public class WorkflowEntity
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = string.Empty;
    public string? ModifiedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Semantic version of this workflow (e.g. "1.2.3").
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    // Navigation property — virtual for lazy loading
    public virtual ICollection<RuleEntity> Rules { get; set; } = new List<RuleEntity>();

    /// <summary>
    /// Converts this EF entity to the domain model with sealed Rules.
    /// </summary>
    public Models.Workflow ToDomainModel()
    {
        var workflow = new Models.Workflow
        {
            Id = Id,
            Description = Description,
            IsActive = IsActive,
            ModifiedBy = ModifiedBy,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt,
            Version = Models.RuleVersion.Parse(Version)
        };

        foreach (var ruleEntity in Rules.Where(r => r.ParentRuleId == null))
        {
            workflow.Rules.Add(ruleEntity.ToDomainModel());
        }

        return workflow;
    }
}
