---
layout: default
title: Rule Predicates
parent: API Reference
nav_order: 10
---

[← Back to API Reference](api-reference.md)

# Rule Predicates

Built-in factory methods for common validation patterns. No raw expressions needed.

```csharp
using RoslynRules.Predicates;
```

---

## Available Predicates

| Predicate | Description |
|-----------|-------------|
| `IsNotNull(path)` | Value is not null |
| `IsNull(path)` | Value is null |
| `Equals(path, value)` | Equality comparison |
| `NotEquals(path, value)` | Inequality comparison |
| `GreaterThan(path, value)` | `>` comparison |
| `GreaterThanOrEqual(path, value)` | `>=` comparison |
| `LessThan(path, value)` | `<` comparison |
| `LessThanOrEqual(path, value)` | `<=` comparison |
| `Between(path, min, max)` | Inclusive range check |
| `MatchesRegex(path, pattern)` | Regex match |
| `Contains(path, value)` | String/collection contains |
| `StartsWith(path, value)` | String prefix |
| `EndsWith(path, value)` | String suffix |
| `IsEmpty(path)` | String/collection empty |
| `IsNotEmpty(path)` | String/collection not empty |
| `HasLength(path, min, max)` | Length in range |
| `IsIn(path, values)` | Value in set |
| `IsNotIn(path, values)` | Value not in set |
| `IsTrue(path)` | Boolean true |
| `IsFalse(path)` | Boolean false |
| `IsDateBefore(path, date)` | Date comparison |
| `IsDateAfter(path, date)` | Date comparison |
| `IsDateBetween(path, start, end)` | Date range |
| `IsGuid(path)` | Valid GUID format |
| `IsEmail(path)` | Valid email format |
| `IsUrl(path)` | Valid URL format |

---

## Usage

```csharp
var workflow = new Workflow
{
    Rules =
    {
        RulePredicates.IsNotNull("customer"),
        RulePredicates.GreaterThan("customer.Age", 18),
        RulePredicates.MatchesRegex("customer.Email", @"^[^@]+@[^@]+\.[^@]+$"),
        RulePredicates.IsIn("customer.Status", new[] { "Active", "Pending" })
    }
};
```

---

## Custom Predicates

Create your own by instantiating `Rule` with the appropriate `Expression`:

```csharp
public static Rule HasMinimumOrders(string path, int min)
{
    return new Rule
    {
        Description = $"{path} has at least {min} orders",
        Expression = $"{path}.Orders.Count >= {min}"
    };
}
```

---

## Related

- [Rule](rule.md) — Underlying model
