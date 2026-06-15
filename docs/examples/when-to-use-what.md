---
layout: default
title: When to Use What
parent: Examples
nav_order: 10
---

[← Back to Examples Index](index.md)

# When to Use What

Choose the right tool for your rules evaluation scenario.

## Decision Matrix

| Scenario | Use | Not | Why |
|----------|-----|-----|-----|
| Single boolean check | `Rule` | `Workflow` | No overhead of workflow |
| 2-5 related checks | `Workflow` | `RuleBatch` | Grouped logic, shared compilation |
| 10+ independent checks | `RuleBatch` | `Workflow` | Shared compile, parallel eval |
| Need streaming results | `Workflow.ExecuteAsync` | `Workflow.Execute` | Memory efficient |
| CPU-intensive expressions | `ExecuteParallel` | `Execute` | Parallel evaluation |
| Async I/O in rules | `ExecuteParallelAsync` | `Execute` | Proper async/await |
| Dynamic rule loading | `RuleBatch` | `Workflow` | Easy to add/remove rules |
| Rules stored in database | `Workflow` + EF | `RuleBatch` | Navigation properties |
| Parent-child logic | `Workflow` with children | `RuleBatch` | Bottom-up evaluation |
| Multi-stage pipeline | `Workflow` with DependsOn | `RuleBatch` | Dependency ordering |
| Real-time progress | `ExecuteAsync` | `Execute` | Yield results as ready |
| Batch processing | `ExecuteBufferedAsync` | `Execute` | Fixed memory footprint |

## Individual Rule

Use when you have a single, standalone condition.

```csharp
// Simple validation
var ageCheck = new Rule
{
    Description = "Age check",
    Expression = "customer.Age >= 18",
    IsActive = true
};

ageCheck.Compile(parameters);
var result = ageCheck.Execute(parameters);

// result.Success tells you pass/fail
```

**When to use:**
- One-off validations
- Simple boolean checks
- Testing expressions before adding to workflow

**When NOT to use:**
- Multiple related checks (use Workflow)
- Need parallel evaluation (use RuleBatch)

## Workflow

Use when you have multiple related rules that form a cohesive unit.

```csharp
var workflow = new Workflow
{
    Description = "Customer onboarding",
    Rules = new List<Rule>
    {
        new Rule { Description = "Age", Expression = "customer.Age >= 18" },
        new Rule { Description = "Email", Expression = "customer.Email.Contains("@")" },
        new Rule { Description = "Country", Expression = "customer.Country != null" }
    }
};

workflow.Validate();
workflow.Compile(parameters);
var results = workflow.Execute(parameters);
```

**When to use:**
- Multiple rules that validate the same entity
- Parent-child relationships needed
- Dependency chains (DependsOnRuleId)
- Need workflow-level activation (IsActive)
- Rules stored in database (EF navigation)

**When NOT to use:**
- Rules from multiple sources (use RuleBatch)
- 10+ rules that don't share context (use RuleBatch)

## RuleBatch

Use when you have many independent rules, possibly from multiple sources.

```csharp
var batch = new RuleBatch();

// Add from code
batch.AddRule(new Rule { Description = "Age", Expression = "customer.Age >= 18" });

// Add from database
var dbRules = await dbContext.Rules.Where(r => r.IsActive).ToListAsync();
batch.AddRules(dbRules);

// Add from JSON
var jsonRules = JsonRuleLoader.LoadWorkflowFromFile("rules.json");
batch.AddRules(jsonRules.Rules);

// Single compile
batch.Compile(parameters);

// Parallel evaluation
var results = batch.EvaluateParallel(parameters);
```

**When to use:**
- 10+ rules
- Rules from multiple sources (DB, JSON, code)
- Need to evaluate as a group
- Maximum performance with parallel execution

**When NOT to use:**
- Rules have dependencies (use Workflow)
- Need parent-child relationships (use Workflow)
- Need streaming (use Workflow.ExecuteAsync)

## Execution Modes

### Sequential (Execute)

```csharp
// Best for: Few rules, predictable order, low overhead
foreach (var result in workflow.Execute(parameters))
{
    Console.WriteLine($"{result.RuleDescription}: {result.Success}");
}
```

- Rules execute one at a time
- Results in priority order
- Lowest overhead
- Use when: < 10 rules, simple expressions

### Parallel (ExecuteParallel)

```csharp
// Best for: Many CPU-intensive rules
var results = workflow.ExecuteParallel(parameters);
```

- All rules execute concurrently
- Results in rule order (not completion order)
- Higher throughput for CPU-bound work
- Use when: > 10 rules, complex expressions, no dependencies

### Async Streaming (ExecuteAsync)

```csharp
// Best for: Many rules, UI updates, early exit
await foreach (var result in workflow.ExecuteAsync(parameters))
{
    UpdateUI(result);
    if (!result.Success && result.RuleDescription == "Critical")
        break;
}
```

- Results yielded as they complete
- Can break early
- Memory efficient
- Use when: > 50 rules, real-time progress, UI updates

### Parallel Async (ExecuteParallelAsync)

```csharp
// Best for: Async I/O in rules (HTTP, DB)
var results = await workflow.ExecuteParallelAsync(parameters);
```

- Concurrent async execution
- Properly awaits async expressions
- Maximum throughput for I/O-bound rules
- Use when: Rules call external APIs, database queries

### Buffered (ExecuteBufferedAsync)

```csharp
// Best for: Large rule sets, batch processing
await foreach (var chunk in workflow.ExecuteBufferedAsync(parameters, bufferSize: 100))
{
    await SaveChunkToDatabase(chunk);
}
```

- Fixed-size chunks
- Constant memory usage
- Batch operations
- Use when: > 1000 rules, batch processing

## Choosing by Rule Count

| Count | Recommended | Execution |
|-------|-------------|-----------|
| 1 | `Rule` | `Execute()` |
| 2-5 | `Workflow` | `Execute()` |
| 5-20 | `Workflow` | `ExecuteParallel()` |
| 20-100 | `Workflow` or `RuleBatch` | `ExecuteParallel()` or `ExecuteAsync()` |
| 100-1000 | `RuleBatch` | `ExecuteBufferedAsync()` |
| 1000+ | `RuleBatch` | `ExecuteBufferedAsync()` with large buffer |

## Choosing by Workload Type

| Workload | Recommended | Why |
|----------|-------------|-----|
| Simple boolean checks | `Execute()` | Low overhead |
| CPU-intensive math | `ExecuteParallel()` | Utilize all cores |
| Database queries | `ExecuteParallelAsync()` | Async I/O throughput |
| HTTP API calls | `ExecuteParallelAsync()` | Concurrent requests |
| File processing | `ExecuteBufferedAsync()` | Memory management |
| Real-time UI | `ExecuteAsync()` | Progressive updates |

## Examples

### Example 1: Form Validation (5 rules)

```csharp
// Use Workflow + sequential execution
var workflow = new Workflow
{
    Rules = new List<Rule>
    {
        new Rule { Description = "Email", Expression = "..." },
        new Rule { Description = "Password", Expression = "..." },
        new Rule { Description = "Age", Expression = "..." },
        new Rule { Description = "Phone", Expression = "..." },
        new Rule { Description = "Terms", Expression = "..." }
    }
};

workflow.Compile(parameters);
var results = workflow.Execute(parameters); // Sequential is fine for 5 rules
```

### Example 2: Health Checks (50 rules)

```csharp
// Use RuleBatch + parallel async
var batch = new RuleBatch();
foreach (var service in services)
{
    batch.AddRule(new Rule
    {
        Description = $"{service.Name} health",
        Expression = $"await HealthClient.CheckAsync(\"{service.Url}\")"
    });
}

batch.Compile(parameters);
var results = await batch.EvaluateParallelAsync(parameters); // Parallel async for HTTP checks
```

### Example 3: Transaction Screening (200 rules)

```csharp
// Use Workflow + buffered execution
var workflow = await LoadWorkflowFromDatabaseAsync("fraud-detection");

await foreach (var chunk in workflow.ExecuteBufferedAsync(parameters, bufferSize: 50))
{
    await ProcessFraudChunk(chunk);
}
```

### Example 4: Feature Flags (10 rules)

```csharp
// Use Workflow + sequential (flags are simple)
var workflow = new Workflow
{
    Rules = featureFlagDefinitions.Select(fd => new Rule
    {
        Description = fd.Name,
        Expression = fd.Expression,
        Action = $"user.EnableFeature(\"{fd.Name}\")"
    }).ToList()
};

workflow.Compile(parameters);
var results = workflow.Execute(parameters);
```

## See Also

- [Workflow API](../api-reference.md#workflow)
- [RuleBatch API](../api-reference.md#rulebatch)
- [Performance Tuning](../performance.md)
