---
layout: default
title: Delegate Types
parent: API Reference
nav_order: 7
---

[← Back to API Reference](api-reference.md)

# Delegate Types

RoslynRules supports exactly **one input parameter**. Return multiple values by wrapping them in a struct or class.

---

## Supported Signatures

| Type | Delegate | Example |
|------|----------|---------|
| **Expression** | `Func<TParam, bool>` | `Func<Customer, bool>` |
| **Expression (composite return)** | `Func<TParam, TReturn>` | `Func<Customer, ValidationResult>` |
| **Action** | `Action<TParam>` | `Action<Customer>` |
| **Async Expression** | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| **Async Action** | `Func<TParam, Task>` | `Func<Customer, Task>` |

---

## Auto-Detection

RoslynRules automatically detects `await` in expressions and compiles to the appropriate async delegate.

```csharp
// Sync — compiled as Func<Customer, bool>
"customer.Age >= 18"

// Async — compiled as Func<Customer, Task<bool>>
"await GetPriceAsync(customer.ProductId) > 100"
```

---

## Multi-Value Return

Return composite data by wrapping in a record or class.

```csharp
public record ValidationResult(bool IsValid, string[] Errors);

// Expression returns ValidationResult
"new ValidationResult(customer.Age >= 18, customer.Age < 18 ? new[] { \"Too young\" } : Array.Empty<string>())"
```

---

## Related

- [ExpressionCompiler](expressioncompiler.md) — Compilation API
- [RuleParameter](ruleparameter.md) — Parameter definition
