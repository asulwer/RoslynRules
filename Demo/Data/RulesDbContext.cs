using Microsoft.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore.Entities;

namespace Demo.Data;

public class RulesDbContext : DbContext
{
    public DbSet<WorkflowEntity> Workflows { get; set; } = null!;
    public DbSet<RuleEntity> Rules { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseInMemoryDatabase("RulesDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureRoslynRules();
    }
}
