---
layout: default
title: Performance Tuning
parent: Documentation
nav_order: 6
---

[<- Back to Documentation Index](index.md)

# Performance Tuning

Fine-tune RoslynRules for your workload.

---

## Compilation Tuning

### maxCompilesBeforeRecycle

Controls how many compilations occur before the `AssemblyLoadContext` is recycled. Lower = more frequent recycling, higher memory stability. Higher = less overhead.

```csharp
// Default: 1000
var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 1000);

// High-throughput, short-lived expressions: reduce to recycle more often
var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 100);

// Long-running, stable rules: increase to reduce recycling overhead
var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 10000);
```

| Value | Use Case |
|-------|----------|
| 100 | Rapidly changing rules, dev/testing |
| 1000 | Default, balanced (recommended) |
| 10000 | Stable production rules, minimal churn |
| 0 | Never auto-recycle; manual `Unload()` only |

### Manual Unload

Force immediate ALC recycling:

```csharp
compiler.Unload();  // Clears cache, unloads assemblies
GC.Collect();
GC.WaitForPendingFinalizers();
```

---

## Execution Tuning

### CacheDuration

Cache rule results to avoid re-evaluating identical inputs:

```csharp
var rule = new Rule
{
    Expression = "customer.CreditScore > 700",
    CacheDuration = TimeSpan.FromMinutes(5)  // Cache for 5 minutes
};
```

**When to use:**
- Expensive expressions (database calls, calculations)
- Stable inputs that don't change frequently
- Read-heavy workloads

**When NOT to use:**
- Real-time decisions requiring fresh evaluation
- Expressions with side effects

### Timeout

Prevent runaway expressions from hanging:

```csharp
var rule = new Rule
{
    Expression = "HeavyComputation(customer)",
    Timeout = TimeSpan.FromSeconds(2)
};
```

| Scenario | Recommended Timeout |
|----------|-------------------|
| Simple boolean | None (default) |
| Database query | 5-10 seconds |
| External API call | 10-30 seconds |
| Complex calculation | 1-5 seconds |

---

## Execution Strategy

Choose the right execution method:

| Method | Best For | Overhead |
|--------|----------|----------|
| `Execute()` | Few simple rules | Lowest |
| `ExecuteParallel()` | Many CPU-intensive rules | Thread pool |
| `ExecuteAsync()` | Async I/O rules | Task overhead |
| `ExecuteParallelAsync()` | Many async I/O rules | Highest throughput |
| `ExecuteBufferedAsync()` | Large rule sets | Batched parallelism |

```csharp
// 5 simple rules: sequential
workflow.Execute(parameters);

// 50 complex rules: parallel
workflow.ExecuteParallel(parameters);

// 100 rules hitting HTTP APIs: parallel async
await workflow.ExecuteParallelAsync(parameters);

// 1000 rules: batched
await foreach (var batch in workflow.ExecuteBufferedAsync(parameters, bufferSize: 100))
{
    ProcessBatch(batch);
}
```

---

## Memory Tuning

### Compiled Delegate Size

- Simple boolean: ~2KB
- Complex expression with method calls: ~5-10KB
- Action with assignments: ~3-5KB

### Rule Object Size

- Base Rule: ~200 bytes
- With events/logger: ~400 bytes
- RuleResult: ~32 bytes

### Typical Workloads

| Rules | Compiled Size | Execution Time |
|-------|----------------|----------------|
| 10 | ~20KB | <1ms |
| 100 | ~200KB | ~2ms |
| 1000 | ~2MB | ~20ms |

---

## Multi-Parameter Optimization

Multi-parameter rules use `DynamicInvoke` (slower than direct calls). For hot paths, prefer single-parameter wrappers:

```csharp
// Slower: multi-parameter
var rule = new Rule { Expression = "price > 0 && quantity > 0" };

// Faster: single wrapper parameter (if called frequently)
public record OrderInput(decimal Price, int Quantity);
var rule = new Rule { Expression = "input.Price > 0 && input.Quantity > 0" };
```

---

## Benchmarking

Measure before optimizing:

```csharp
var stopwatch = Stopwatch.StartNew();
var results = workflow.ExecuteParallel(parameters);
stopwatch.Stop();

Console.WriteLine($"Executed {workflow.Rules.Count} rules in {stopwatch.ElapsedMilliseconds}ms");
```

---

## Related

- [Performance](performance.md) — Benchmarks and comparisons
- [API: ExpressionCompiler](api/expressioncompiler.md)
- [API: Rule](api/rule.md) — `CacheDuration`, `Timeout`
- [API: Workflow](api/workflow.md) — Execution methods
