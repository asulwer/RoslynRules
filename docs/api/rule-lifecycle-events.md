---
layout: default
title: RuleLifecycleEvents
parent: API Reference
nav_order: 8
---

[← Back to API Reference](api-reference.md)

# RuleLifecycleEvents

Event arguments for the `OnRuleExecuting` and `OnRuleExecuted` events on `Rule`. These hooks let you intercept, monitor, and control rule execution.

```csharp
public class RuleExecutingEventArgs : EventArgs
public class RuleExecutedEventArgs : EventArgs
```

**Namespace:** `RoslynRules.Models`

**Key characteristics:**
- `RuleExecutingEventArgs` is **pre-execution** — set `Cancel = true` to skip
- `RuleExecutedEventArgs` is **post-execution** — read-only result data
- Both inherit from `EventArgs` for standard .NET event compatibility
- `CancelReason` provides optional context when skipping a rule

---

## RuleExecutingEventArgs

Fired before a rule executes. Allows cancellation and provides access to the rule and its parameters.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Rule` | `Rule` | The rule about to execute (read-only) |
| `Parameters` | `RuleParameter[]` | The parameters passed to the rule (read-only) |
| `Cancel` | `bool` | When `true`, skips rule execution and returns a `RuleResult` with `Success = true` |
| `CancelReason` | `string?` | Optional reason logged when cancellation occurs |

### Constructor

```csharp
public RuleExecutingEventArgs(Rule rule, RuleParameter[] parameters)
```

Throws `ArgumentNullException` if `rule` or `parameters` is null.

---

## RuleExecutedEventArgs

Fired after a rule completes execution, regardless of success or failure.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Rule` | `Rule` | The rule that was executed (read-only) |
| `Result` | `RuleResult` | The result of rule execution (read-only) |
| `Elapsed` | `TimeSpan` | Time spent executing the rule (excluding child rules) |
| `Exception` | `Exception?` | Exception thrown during execution, if any |

### Constructor

```csharp
public RuleExecutedEventArgs(
    Rule rule,
    RuleResult result,
    TimeSpan elapsed,
    Exception? exception = null)
```

Throws `ArgumentNullException` if `rule` is null.

---

## Usage Example

### Pre-execution: Skip rules conditionally

```csharp
var rule = new Rule
{
    Description = "Check admin privileges",
    Expression = "user.IsAdmin"
};

rule.OnRuleExecuting += (sender, args) =>
{
    Console.WriteLine($"About to execute: {args.Rule.Description}");
    Console.WriteLine($"Parameters: {string.Join(", ", args.Parameters.Select(p => $"{p.Name}={p.Value}"))}");

    // Skip execution during maintenance windows
    if (IsMaintenanceWindow())
    {
        args.Cancel = true;
        args.CancelReason = "System maintenance in progress";
    }

    // Skip if user is in bypass list
    var userParam = args.Parameters.FirstOrDefault(p => p.Name == "user");
    if (userParam?.Value is User user && bypassList.Contains(user.Id))
    {
        args.Cancel = true;
        args.CancelReason = "User in bypass list";
    }
};
```

### Post-execution: Audit and metrics

```csharp
rule.OnRuleExecuted += (sender, args) =>
{
    var status = args.Result.Success ? "PASS" : "FAIL";
    Console.WriteLine($"[{status}] {args.Rule.Description} — {args.Elapsed.TotalMilliseconds:0.00}ms");

    if (args.Exception != null)
    {
        Console.WriteLine($"  Exception: {args.Exception.Message}");
    }

    // Emit to metrics pipeline
    metrics.RecordRuleExecution(
        ruleId: args.Rule.Id,
        success: args.Result.Success,
        durationMs: args.Elapsed.TotalMilliseconds,
        exception: args.Exception?.Message);
};
```

### Full workflow example

```csharp
public class AuditedWorkflow
{
    private readonly Workflow _workflow;
    private readonly ILogger<AuditedWorkflow> _logger;

    public AuditedWorkflow(Workflow workflow, ILogger<AuditedWorkflow> logger)
    {
        _workflow = workflow;
        _logger = logger;

        foreach (var rule in workflow.Rules)
        {
            AttachHooks(rule);
        }
    }

    private void AttachHooks(Rule rule)
    {
        rule.OnRuleExecuting += (s, e) =>
        {
            _logger.LogDebug("Executing rule {RuleId}: {Description}",
                e.Rule.Id, e.Rule.Description);
        };

        rule.OnRuleExecuted += (s, e) =>
        {
            var level = e.Exception != null ? LogLevel.Error :
                        e.Result.Success ? LogLevel.Debug : LogLevel.Warning;

            _logger.Log(level, "Rule {RuleId} completed in {ElapsedMs:0.00}ms: {Success}",
                e.Rule.Id, e.Elapsed.TotalMilliseconds, e.Result.Success);
        };

        // Recurse into children
        foreach (var child in rule.ChildRules)
            AttachHooks(child);
    }
}
```

---

## Related

- [Rule](rule.md) — Individual rule API (events defined here)
- [RuleResult](ruleresult.md) — Execution result structure
- [RuleDiagnostics](rule-diagnostics.md) — Structured logging model for execution events
- [Lifecycle Events](lifecycle-events.md) — Higher-level lifecycle documentation
