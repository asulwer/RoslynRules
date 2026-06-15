---
layout: default
title: Workflow
parent: API Reference
nav_order: 2
---

[← Back to API Reference](api-reference.md)

# Workflow

Container for top-level rules. Owns compilation, validation, and execution orchestration.

```csharp
public class Workflow : IRuleEngine
```

---

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `Description` | `string` | Human-readable purpose |
| `IsActive` | `bool` | When `false`, entire workflow is skipped |
| `Rules` | `IList<Rule>` | Top-level rules |

---

## Methods

### `Validate()` / `ValidateAll()`

Validates all rules and checks workflow consistency.

```csharp
workflow.Validate();     // Throws on errors
workflow.ValidateAll();  // Returns ValidationError[]
```

### `Compile(RuleParameter[], string[]?)`

Compiles all active rules. Uses an internal `ExpressionCompiler`.

```csharp
// Compile: only types matter, values can be null
var compileParams = new[] { new RuleParameter("customer", typeof(Customer)) };
workflow.Compile(compileParams);

// Execute: real values required
var executeParams = new[] { new RuleParameter("customer", typeof(Customer), customer) };
var results = workflow.Execute(executeParams);
```

### `Execute(params RuleParameter[])`

Sequential execution in priority order.

```csharp
var results = workflow.Execute(parameters);
```

### `ExecuteParallel(params RuleParameter[])`

Parallel execution. Rules with dependencies execute after their dependencies complete.

```csharp
var results = workflow.ExecuteParallel(parameters);
```

### `ExecuteAsync(params RuleParameter[], CancellationToken)`

Async streaming with cancellation support.

```csharp
await foreach (var result in workflow.ExecuteAsync(parameters, cts.Token))
{
    if (!result.Success) break; // Short-circuit
}
```

### `ExecuteParallelAsync(params RuleParameter[], CancellationToken)`

Parallel async execution.

```csharp
var results = await workflow.ExecuteParallelAsync(parameters, cts.Token);
```

### `ExecuteBufferedAsync(params RuleParameter[], int bufferSize, CancellationToken)`

Chunked streaming for large rule sets.

```csharp
await foreach (var chunk in workflow.ExecuteBufferedAsync(parameters, bufferSize: 10))
{
    ProcessBatch(chunk);
}
```

---

## Execution Modes Comparison

| Mode | Use Case | Overhead |
|------|----------|----------|
| `Execute` | Default, predictable order | Lowest |
| `ExecuteParallel` | CPU-intensive rules, many independent rules | Medium (thread scheduling) |
| `ExecuteAsync` | Async I/O in rules, streaming results | Low |
| `ExecuteParallelAsync` | Async + parallel combined | Highest |
| `ExecuteBufferedAsync` | Large rule sets, chunked processing | Low |

---

## Related

- [Rule](rule.md) — Individual rule API
- [RuleBatch](rulebatch.md) — Alternative batch execution
- [IRuleEngine](iruleengine.md) — Abstraction interface
