---
layout: default
title: Delegate Types
parent: API Reference
nav_order: 7
---

[← Back to API Reference](../api-reference.md)

# Delegate Types

RoslynRules supports **up to 16 input parameters** per expression or action. Single-parameter
signatures take a fast, strongly-typed path; multi-parameter signatures (2–16 parameters) are
supported as well. To return multiple values from a single rule, wrap them in a struct or class.

---

## Supported Signatures

Single-parameter signatures (fastest path):

| Type | Delegate | Example |
|------|----------|---------|
| **Expression** | `Func<TParam, bool>` | `Func<Customer, bool>` |
| **Expression (composite return)** | `Func<TParam, TReturn>` | `Func<Customer, ValidationResult>` |
| **Action** | `Action<TParam>` | `Action<Customer>` |
| **Async Expression** | `Func<TParam, Task<bool>>` | `Func<Customer, Task<bool>>` |
| **Async Action** | `Func<TParam, Task>` | `Func<Customer, Task>` |

Multi-parameter signatures (up to 16 parameters) follow the same shapes:

| Type | Delegate | Example |
|------|----------|---------|
| **Expression** | `Func<T1, …, Tn, bool>` | `Func<Customer, Order, bool>` |
| **Action** | `Action<T1, …, Tn>` | `Action<Customer, Order>` |
| **Async Expression** | `Func<T1, …, Tn, Task<bool>>` | `Func<Customer, Order, Task<bool>>` |
| **Async Action** | `Func<T1, …, Tn, Task>` | `Func<Customer, Order, Task>` |

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
