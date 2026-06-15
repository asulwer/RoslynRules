---
layout: default
title: Result Caching
parent: API Reference
nav_order: 16
---

[← Back to API Reference](api-reference.md)

# Result Caching (Memoization)

Cache rule evaluation results to skip redundant execution.

---

## Enabling Cache

```csharp
var rule = new Rule
{
    Description = "Complex validation",
    Expression = "ExpensiveOperation(customer)",
    CacheDuration = TimeSpan.FromMinutes(5)
};
```

**Behavior:**
- Opt-in per-rule via `CacheDuration` (`null` = disabled)
- Thread-safe via `ConcurrentDictionary`
- Lazy expiration on read
- Cache key = rule ID + all parameter values
- Only final result cached; children evaluated independently
- Exceptions **not** cached — subsequent calls re-evaluate

---

## Clearing Cache

```csharp
rule.ClearCache();  // Force next evaluation to re-run
```

---

## Recommendations

| Scenario | Duration |
|----------|----------|
| Database lookups | 30s – 5min |
| API calls | 1 – 10min |
| CPU-intensive calculations | 1 – 60min |
| Static reference data | Hours or until `ClearCache()` |

---

## Important

Enable only for **idempotent** rules — rules whose result won't change for the same input during the cache window.

---

## Related

- [Rule.CacheDuration](rule.md#properties) — Property
- [Rule.ClearCache](rule.md#clearcache) — Method
