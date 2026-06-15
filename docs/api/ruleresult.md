---
layout: default
title: RuleResult
parent: API Reference
nav_order: 3
---

[← Back to API Reference](api-reference.md)

# RuleResult

Immutable result from a single rule evaluation. Includes full child evaluation tree for traceability.

```csharp
public readonly record struct RuleResult
```

**Key characteristics:**
- `readonly record struct` — value-based equality, no heap allocation per result
- Contains full child evaluation tree
- Helper properties for finding failures

---

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | `true` if rule passed |
| `RuleId` | `Guid` | GUID of the evaluated rule |
| `RuleDescription` | `string` | Human-readable rule name |
| `IsActive` | `bool` | Whether rule was active during evaluation |
| `Value` | `object?` | Return value from Action (if any) |
| `Exception` | `Exception?` | Exception if execution failed |
| `ChildResults` | `IReadOnlyList<RuleResult>` | Nested results from child rules |
| `FirstFailure` | `RuleResult?` | First failing child (or `null` if all passed) |
| `AllFailures` | `IEnumerable<RuleResult>` | All failing children (recursive) |

---

## Usage Examples

### Basic Check

```csharp
var result = rule.Execute(parameters);
if (!result.Success)
    Console.WriteLine($"Failed: {result.RuleDescription}");
```

### Find First Failure

```csharp
var failure = result.FirstFailure;
Console.WriteLine($"Caused by: {failure?.RuleDescription}");
```

### Iterate All Failures

```csharp
foreach (var fail in result.AllFailures)
{
    Console.WriteLine($"Failed: {fail.RuleDescription} (Id: {fail.RuleId})");
}
```

### Child Results

```csharp
foreach (var child in result.ChildResults)
{
    Console.WriteLine($"  [{child.Success}] {child.RuleDescription}");
}
```

---

## Related

- [Rule](rule.md) — Produces RuleResult
- [RuleTest](rule-test.md) — Testing framework assertions
