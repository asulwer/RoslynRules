---
layout: default
title: Rule Priority
parent: API Reference
nav_order: 14
---

[← Back to API Reference](api-reference.md)

# Rule Priority

Control execution order with the `Priority` property. Higher values execute first.

```csharp
public int Priority { get; set; }  // Default: 0
```

---

## Execution Order

| Priority | Order |
|----------|-------|
| `100` | First |
| `10` | Second |
| `0` | Default (third) |
| `-10` | After defaults |

---

## Example

```csharp
var workflow = new Workflow
{
    Rules =
    {
        new Rule { Expression = "true", Description = "Low", Priority = 0 },
        new Rule { Expression = "true", Description = "High", Priority = 10 },
        new Rule { Expression = "true", Description = "Medium", Priority = 5 }
    }
};

// Execution order: High → Medium → Low
var results = workflow.Execute(parameters);
```

---

## Notes

- Priority is **immutable after `Compile()`**
- Works in `Workflow` and `RuleBatch`
- Only affects order within the same container; dependencies still execute first

---

## Related

- [Rule](rule.md) — Priority property
- [Workflow](workflow.md) — Execution container
