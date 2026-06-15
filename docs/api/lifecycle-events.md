---
layout: default
title: Lifecycle Events
parent: API Reference
nav_order: 15
---

[← Back to API Reference](api-reference.md)

# Lifecycle Events

Attach event handlers to rules for custom logic before and after execution.

---

## OnRuleExecuting

Fires before a rule evaluates. Set `Cancel = true` to skip.

```csharp
rule.OnRuleExecuting += (sender, args) =>
{
    Console.WriteLine($"About to execute: {args.Rule.Description}");
    
    if (args.Parameters[0].Value is Customer c && c.Name == "SkipMe")
    {
        args.Cancel = true;
        args.CancelReason = "Customer opted out";
    }
};
```

**When cancelled:**
- Rule returns `Success = true` (skipped, not failed)
- `OnRuleExecuted` still fires with cancellation info
- `CancelReason` embedded as `OperationCanceledException`

---

## OnRuleExecuted

Fires after a rule completes — success, failure, or cancellation.

```csharp
rule.OnRuleExecuted += (sender, args) =>
{
    Console.WriteLine($"Rule {args.Rule.Description}: {(args.Result.Success ? "PASS" : "FAIL")}");
    Console.WriteLine($"Elapsed: {args.Elapsed.TotalMilliseconds}ms");
    
    if (args.Exception != null)
        Console.WriteLine($"Exception: {args.Exception.Message}");
};
```

**Event args:**
- `args.Rule` — The rule that executed
- `args.Result` — Full `RuleResult`
- `args.Elapsed` — Execution time
- `args.Exception` — Exception if failed

---

## Use Cases

| Use Case | Handler |
|----------|---------|
| Audit logging | `OnRuleExecuted` — write to audit table |
| Circuit breaker | `OnRuleExecuting` — cancel if unhealthy |
| Metrics | `OnRuleExecuted` — record timing |
| Feature flags | `OnRuleExecuting` — conditional skip |
| Debugging | Both — trace evaluation flow |

---

## Child Rule Events

Child rules fire their own events independently.

```csharp
child.OnRuleExecuting += (s, e) => { /* fires when child evaluates */ };
parent.OnRuleExecuting += (s, e) => { /* fires when parent evaluates */ };
```

---

## Related

- [Rule](rule.md) — Event properties
