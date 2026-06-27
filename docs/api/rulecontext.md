---
layout: default
title: RuleContext
parent: API Reference
nav_order: 11
---

[← Back to API Reference](../api-reference.md)

# RuleContext

Provides access to dependency rule results during execution. Used with `DependsOnRuleId`.

```csharp
public class RuleContext
```

---

## Methods

### `GetResult(Guid)`

Gets the `RuleResult` for a specific rule ID. Returns `RuleResult?` (`null` if the rule has
not executed yet).

```csharp
RuleResult? dependencyResult = context.GetResult(validateCustomer.Id);
if (dependencyResult is { Success: true })
{
    // dependency executed and passed
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

`RuleContext` is a **host-side** API. There is no `context` variable in scope inside an
`Expression` or `Action` string — those strings may only reference declared `RuleParameter`s.
The host code reads dependency results from the context after (or between) rule execution.

```csharp
var taxRule = new Rule
{
    Description = "Calculate tax",
    // Expressions/actions reference declared parameters only — never "context".
    Action = "customer.TaxAmount = customer.Amount * 0.08m"
};

var totalRule = new Rule
{
    Description = "Calculate total",
    DependsOnRuleId = taxRule.Id,
    Action = "customer.Total = customer.Amount + customer.TaxAmount"
};

// Host code inspects dependency results via RuleContext.
var context = new RuleContext();
// ... engine stores each rule's result into the context as it runs ...

if (context.TryGetValue<decimal>(taxRule.Id, out var tax))
{
    Console.WriteLine($"Tax computed by dependency: {tax}");
}

RuleResult? taxResult = context.GetResult(taxRule.Id);
if (taxResult is { Success: true })
{
    // proceed knowing the tax rule succeeded
}
```

---

## Related

- [Rule.DependsOnRuleId](rule.md#properties) — Dependency declaration
- [Rule.ExecuteWithContext](rule.md#executewithcontext) — Execution with context
