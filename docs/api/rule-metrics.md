---
layout: default
title: Rule Metrics
description: Per-rule execution statistics
parent: API Reference
nav_order: 17
---

[← Back to API Reference](../api-reference.md)

# Rule Metrics

Track per-rule execution statistics for monitoring, debugging, and performance optimization.

## Overview

Every `Rule` exposes a `Metrics` property that accumulates statistics atomically during execution:

| Metric | Description |
|--------|-------------|
| `EvalCount` | Total number of evaluations |
| `FailureCount` | Number of failed evaluations |
| `AverageExecutionTimeMs` | Mean execution time |
| `FailureRatePercent` | Failure percentage (0–100) |
| `LastExecuted` | UTC timestamp of last run |

## Quick Start

```csharp
var rule = new Rule
{
    Description = "Adult check",
    Expression = "customer.Age >= 18",
    IsActive = true
};

var parameters = new[] { new RuleParameter("customer", typeof(Customer), customer) };
var workflow = new Workflow { Rules = { rule } };
workflow.Compile(parameters);

// Execute multiple times
workflow.Execute(parameters);
workflow.Execute(parameters);
workflow.Execute(parameters);

// Check metrics
Console.WriteLine($"Evaluated: {rule.Metrics.EvalCount} times");
Console.WriteLine($"Avg time: {rule.Metrics.AverageExecutionTimeMs:F2} ms");
Console.WriteLine($"Failure rate: {rule.Metrics.FailureRatePercent:F1}%");
Console.WriteLine($"Last run: {rule.Metrics.LastExecuted}");
```

## Thread Safety

Metrics update via lock-free `Interlocked` operations. Safe for concurrent execution:

```csharp
Parallel.For(0, 1000, _ => workflow.Execute(parameters).ToList());

Console.WriteLine(rule.Metrics.EvalCount); // 1000
```

## Failure Detection

A "failure" is either:
- Expression evaluates to `false`
- Exception thrown during execution

```csharp
var badRule = new Rule { Expression = "x > 0" };
var parameters = new[] { new RuleParameter("x", typeof(int), -1) };
var workflow = new Workflow { Rules = { badRule } };
workflow.Compile(parameters);

workflow.Execute(parameters); // fails: -1 > 0 is false
Console.WriteLine(badRule.Metrics.FailureRatePercent); // 100
```

## Reset

`ClearCache()` also resets metrics:

```csharp
rule.ClearCache();
Console.WriteLine(rule.Metrics.EvalCount); // 0
```

---

## Related

- [Rule](rule.md) — `Metrics` property
- [Result Caching](result-caching.md) — Cache and metrics interaction
- [Performance Tuning](../performance-tuning.md) — Optimize based on metrics


## Dashboard Integration

Export metrics for monitoring:

```csharp
var report = workflow.Rules.Select(r => new
{
    r.Description,
    r.Metrics.EvalCount,
    r.Metrics.AverageExecutionTimeMs,
    r.Metrics.FailureRatePercent
});

foreach (var item in report)
{
    Console.WriteLine(
        $"{item.Description}: {item.EvalCount} evals, " +
        $"{item.AverageExecutionTimeMs:F2}ms avg, " +
        $"{item.FailureRatePercent:F1}% fail");
}
```
