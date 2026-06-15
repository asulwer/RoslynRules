---
layout: default
title: IRuleEngine
parent: API Reference
nav_order: 12
---

[← Back to API Reference](api-reference.md)

# IRuleEngine

Abstraction for dependency injection and unit testing.

```csharp
public interface IRuleEngine
```

---

## Methods

| Method | Returns | Use Case |
|--------|---------|----------|
| `Compile(params, namespaces?)` | `void` | One-time compilation |
| `Execute(params)` | `IEnumerable<RuleResult>` | Sequential execution |
| `ExecuteAsync(params, ct)` | `IAsyncEnumerable<RuleResult>` | Streaming async |
| `ExecuteParallel(params)` | `RuleResult[]` | CPU-bound parallel |
| `ExecuteParallelAsync(params, ct)` | `Task<RuleResult[]>` | Async parallel |
| `Validate()` | `void` | Pre-compile validation (throws) |
| `ValidateAll()` | `ValidationError[]` | Pre-compile validation (returns) |

---

## DI Registration

```csharp
services.AddSingleton<IRuleEngine, Workflow>();
// Or
services.AddSingleton<IRuleEngine, RuleBatch>();
```

---

## Mocking with Moq

```csharp
var mock = new Mock<IRuleEngine>();
mock.Setup(x => x.Execute(It.IsAny<RuleParameter[]>()))
    .Returns(new[] { new RuleResult { Success = true } });

var service = new MyService(mock.Object);
```

---

## Implementations

| Class | When to Use |
|-------|-------------|
| `Workflow` | General-purpose rule container |
| `RuleBatch` | 10+ rules with shared compilation context |

---

## Related

- [Workflow](workflow.md) — Default implementation
- [RuleBatch](rulebatch.md) — Batch implementation
