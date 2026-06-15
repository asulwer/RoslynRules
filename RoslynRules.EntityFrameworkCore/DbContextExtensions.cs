using Microsoft.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore.Entities;

namespace RoslynRules.EntityFrameworkCore;

public static class DbContextExtensions
{
    /// <summary>
    /// Configures EF Core entity mapping for RoslynRules entities.
    /// Call this in your DbContext.OnModelCreating.
    /// </summary>
    public static void ConfigureRoslynRules(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowEntity>(w =>
        {
            w.HasKey(x => x.Id);
            w.HasMany(x => x.Rules)
             .WithOne(r => r.Workflow)
             .HasForeignKey(r => r.WorkflowId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleEntity>(r =>
        {
            r.HasKey(x => x.Id);
            r.HasMany(x => x.ChildRules)
             .WithOne(c => c.ParentRule)
             .HasForeignKey(c => c.ParentRuleId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
