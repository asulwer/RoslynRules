---
layout: default
title: RuleContext
parent: API Reference
nav_order: 11
---

[← Back to API Reference](api-reference.md)

# RuleContext

Provides access to dependency rule results during execution. Used with `DependsOnRuleId`.

```csharp
public class RuleContext
```

---

## Methods

### `GetResult(Guid)`

Gets the `RuleResult` for a specific rule ID.

```csharp
var dependencyResult = context.GetResult(validateCustomer.Id);
if (dependencyResult.Success)
{
    // dependency passed
}
```

### `GetValue<T>(Guid)`

Gets the typed `Value` from a rule's result.

```csharp
int taxAmount = context.GetValue<int>(taxRule.Id);
```

⚠️ **Caution:** Returns `default(T)` when rule not found — ambiguous for value types.

### `TryGetValue<T>(Guid, out T)`

Safer alternative that distinguishes "not found" from "value was default".

```csharp
if (context.TryGetValue<int>(taxRule.Id, out var amount))
{
    Console.WriteLine($"Tax: {amount}");
}
else
{
    Console.WriteLine("Tax rule not found or failed");
}
```

### `StoreResult(Guid, RuleResult)`

Stores a result (called internally by the execution engine).

```csharp
context.StoreResult(rule.Id, result);
```

---

## Usage with DependsOnRuleId

```csharp
var taxRule = new Rule
{
    Description = "Calculate tax",
    Action = "customer.TaxAmount = customer.Amount * 0.08"
};

var totalRule = new Rule
{
    Description = "Calculate total",
    DependsOnRuleId = taxRule.Id,
    Expression = "context.TryGetValue<decimal>(taxRule.Id, out var tax)",
    Action = "customer.Total = customer.Amount + tax"
};
```

---

## Related

- [Rule.DependsOnRuleId](rule.md#properties) — Dependency declaration
- [Rule.ExecuteWithContext](rule.md#executewithcontext) — Execution with context
