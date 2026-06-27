---
layout: default
title: Rule Predicates
parent: API Reference
nav_order: 10
---

[← Back to API Reference](../api-reference.md)

# Rule Predicates

Built-in factory methods for common validation patterns. No raw expressions needed.

```csharp
using RoslynRules.Predicates;
```

---

## Available Predicates

All methods are `static` and return a `Rule`. Every method accepts an optional trailing
`string? description = null` argument (omitted below for brevity). The `path` argument is a
parameter name and may be a dotted member-access path (e.g. `"order.CustomerId"`).

### Null / Empty Checks

| Predicate | Description |
|-----------|-------------|
| `IsNotNull(path)` | Value is not null |
| `IsNotNullOrEmpty(path)` | String is not null or empty |
| `IsNotNullOrWhiteSpace(path)` | String is not null or whitespace |
| `IsNotEmpty(path)` | Collection has any elements (`.Any()`) |
| `IsEmpty(path)` | Collection has no elements (`!.Any()`) |

### Comparison

| Predicate | Description |
|-----------|-------------|
| `GreaterThan<T>(path, value)` | `>` comparison (`T : struct`) |
| `GreaterThanOrEqual<T>(path, value)` | `>=` comparison (`T : struct`) |
| `LessThan<T>(path, value)` | `<` comparison (`T : struct`) |
| `LessThanOrEqual<T>(path, value)` | `<=` comparison (`T : struct`) |
| `Equals<T>(path, value)` | `==` comparison |
| `NotEquals<T>(path, value)` | `!=` comparison |
| `InRange<T>(path, min, max)` | Inclusive range check (`T : struct`) |
| `NotInRange<T>(path, min, max)` | Outside exclusive range (`T : struct`) |

### String

| Predicate | Description |
|-----------|-------------|
| `MatchesRegex(path, pattern)` | Regex match |
| `Contains(path, value)` | String contains substring |
| `StartsWith(path, value)` | String prefix |
| `EndsWith(path, value)` | String suffix |
| `HasLength(path, length)` | String has exact length |
| `HasMinLength(path, minLength)` | String length `>=` minLength |
| `HasMaxLength(path, maxLength)` | String length `<=` maxLength |

### Collection

| Predicate | Description |
|-----------|-------------|
| `CountEquals(path, count)` | Collection count `==` count |
| `CountGreaterThan(path, count)` | Collection count `>` count |
| `CountLessThan(path, count)` | Collection count `<` count |
| `Contains<T>(path, value)` | Collection contains element |

### Boolean / Type

| Predicate | Description |
|-----------|-------------|
| `IsTrue(path)` | Boolean is true |
| `IsFalse(path)` | Boolean is false |
| `IsOfType<T>(path)` | Value is of type `T` |

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
        RulePredicates.InRange("customer.Score", 0, 100),
        RulePredicates.HasMinLength("customer.Name", 2)
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
