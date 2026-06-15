---
layout: default
title: Entity Framework Core Integration
parent: Examples
nav_order: 9
---

[← Back to Examples Index](index.md)

# Entity Framework Core Integration

Install the `RoslynRules.EntityFrameworkCore` package:

```bash
dotnet add package RoslynRules.EntityFrameworkCore
```

Store and load rules from a database using Entity Framework Core. The `RoslynRules.EntityFrameworkCore` package provides separate entity models that support lazy loading while keeping the core `Rule` model sealed and immutable.

## Quick Start

### 1. Configure your DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<WorkflowEntity> Workflows { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ConfigureRoslynRules();
    }
}
```

### 2. Store a workflow

```csharp
var workflow = new Workflow
{
    Description = "Validation rules",
    Rules = new List<Rule>
    {
        new Rule { Description = "Adult check", Expression = "customer.Age >= 18" }
    }
};

// Convert to entity and store
var entity = new WorkflowEntity
{
    Description = workflow.Description,
    Rules = workflow.Rules.Select(r => new RuleEntity { ... }).ToList()
};

db.Workflows.Add(entity);
await db.SaveChangesAsync();
```

### 3. Load with lazy loading, convert to domain model

```csharp
// Lazy loading works — RuleEntity is NOT sealed
var entity = await db.Workflows.FirstAsync();

// Convert to sealed domain model for execution
var workflow = entity.ToDomainModel();

workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
var results = workflow.Execute(new[] { new RuleParameter("customer", typeof(Customer), customer) });
```

## Architecture

| Layer | Type | Sealed | Lazy Loading |
|-------|------|--------|-------------|
| `RuleEntity` | EF persistence | No | Yes |
| `Rule` | Domain/execution | Yes | No |

The `ToDomainModel()` method recursively converts the entity graph to sealed `Rule` instances. Once converted, the domain model has full immutability and compilation safety.

## See Also

- [Real-World Use Cases](real-world-use-cases.md)
- [API Reference: Workflow](../api/workflow.md)
- [NuGet Package](https://www.nuget.org/packages/RoslynRules.EntityFrameworkCore)
