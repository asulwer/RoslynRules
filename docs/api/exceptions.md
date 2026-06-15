---
layout: default
title: Exceptions
parent: API Reference
nav_order: 5
---

[← Back to API Reference](api-reference.md)

# Exceptions

All RoslynRules exceptions inherit from `RulesException`.

```csharp
public abstract class RulesException : Exception
```

---

## Exception Hierarchy

| Exception | When Thrown |
|-----------|-------------|
| `RuleValidationException` | Rule has no Expression, Action, or active children |
| `CircularReferenceException` | Circular reference in child rules or `DependsOnRuleId` chain |
| `SyntaxErrorException` | Invalid C# syntax in expression or action |
| `RuleCompilationException` | Roslyn compilation failure |
| `NotCompiledException` | `Execute` called before `Compile` |
| `RuleExecutionException` | Runtime error in compiled code |
| `RuleTimeoutException` | Rule exceeded configured `Timeout` |
| `WorkflowException` | Workflow has no active rules |
| `DuplicateRuleIdException` | Duplicate rule IDs in same workflow |

---

## ValidationError

Non-throwing validation uses `ValidationError`:

```csharp
public record ValidationError(string Message, ValidationErrorType ErrorType, Guid? RuleId = null, string? RuleDescription = null);
```

**Error types:**

| Type | When |
|------|------|
| `NoActiveRules` | Workflow has no active rules |
| `EmptyRule` | Rule has no Expression, Action, or active children |
| `CircularReference` | Circular dependency detected |
| `SyntaxError` | Invalid C# syntax |
| `DuplicateRuleId` | Duplicate rule IDs |
| `MissingDependency` | `DependsOnRuleId` points to non-existent rule |
| `General` | Other validation failure |

---

## Usage

```csharp
var compileParams = new[] { new RuleParameter("customer", typeof(Customer)) };
var executeParams = new[] { new RuleParameter("customer", typeof(Customer), customer) };

try
{
    workflow.Validate();
    workflow.Compile(compileParams);
    var results = workflow.Execute(executeParams);
}
catch (SyntaxErrorException ex)
{
    Console.WriteLine($"Syntax error: {ex.Message}");
}
catch (CircularReferenceException ex)
{
    Console.WriteLine($"Circular ref at rule {ex.RuleId}");
}
catch (RulesException ex)  // Catch-all for any rules error
{
    Console.WriteLine($"Rules error: {ex.Message}");
}
```

---

## Related

- [Rule.Validate()](rule.md#validate) — Validation method
- [Rule.ValidateAll()](rule.md#validateall) — Non-throwing validation
