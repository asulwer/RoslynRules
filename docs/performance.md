---
layout: default
title: Performance
parent: Documentation
nav_order: 5
---

[<- Back to Documentation Index](index.md)

# Performance

## Benchmarks

Typical execution for 999 customers on .NET 8:

| Phase | Time | Per Customer | Notes |
|-------|------|-------------|-------|
| Validation | ~46ms | — | One-time, before compile |
| Compilation | ~812ms | — | One-time, cached per signature |
| Sequential Execute | ~2ms | 0.002ms | Best for few simple rules |
| Parallel Execute | ~3ms | 0.003ms | Best for many CPU-intensive rules |
| Parallel Async | ~5ms | 0.005ms | Best for async I/O rules |

## Why It's Fast

### 1. Roslyn Compilation

Expressions compile to IL once. No interpreter overhead.

### 2. Typed Delegates

```csharp
// RulesEngine (slow)
var result = delegate.DynamicInvoke(args);  // Boxing + reflection

// This engine (fast)
var result = _delegate(customer);  // Direct call, zero overhead
```

### 3. Immutable Rules

Properties locked after `Compile()`. Zero locks, zero contention.

### 4. Parallel.For

Independent rules run on thread pool. No manual Task creation.

## Tuning Tips

| Scenario | Recommendation |
|----------|---------------|
| Few simple rules | Use `Execute()` — parallel overhead not worth it |
| Many complex rules | Use `ExecuteParallel()` |
| Rules hit database/HTTP | Use `ExecuteParallelAsync()` |
| One-time setup | Call `Validate()` + `Compile()` at startup |
| Hot path | Reuse compiled workflow, never recompile |
| Rapidly changing rules | Lower `maxCompilesBeforeRecycle` |
| Stable production rules | Raise `maxCompilesBeforeRecycle` |

## Memory

- Compiled delegates: ~2KB per expression
- Rule objects: ~200 bytes each
- RuleResult: ~32 bytes per execution

Typical 100-rule workflow: ~50KB compiled, executes in microseconds.

## Multi-Parameter Delegates

RoslynRules supports up to 16 parameters directly. Single-parameter delegates use direct invocation (fastest). Multi-parameter delegates use `DynamicInvoke` with an object array (slightly slower).

For maximum performance with multiple inputs, wrap them in a struct:

```csharp
// Fastest: single parameter
public record CustomerInput(int Age, bool IsActive, string Name);
var rule = new Rule { Expression = "input.Age >= 18 && input.IsActive" };

// Convenient: multi-parameter (slightly slower)
var rule = new Rule { Expression = "age >= 18 && isActive" };
```

## See Also

- [Performance Tuning](performance-tuning.md) — Detailed tuning guide
- [Benchmarks](https://github.com/asulwer/RoslynRules/tree/main/RoslynRules.Benchmarks) — BenchmarkDotNet project
