---
layout: default
title: RuleDiagnostics
parent: API Reference
nav_order: 6
---

[← Back to API Reference](api-reference.md)

# RuleDiagnostics

Diagnostics, logging, and auditing model for rule execution. Captures timing, success/failure, and metadata for each evaluated rule.

```csharp
public class RuleExecutedEvent
public static class RuleLoggingExtensions
```

**Namespace:** `RoslynRules.Models`

**Key characteristics:**
- `RuleExecutedEvent` is a **plain data model** — no behavior, just structured properties
- `RuleLoggingExtensions` provides **structured logging** via `Microsoft.Extensions.Logging`
- Works with any `ILogger` implementation (Serilog, NLog, log4net, etc.)
- Uses **event IDs** for filtering and routing: `1001`–`1004`

---

## RuleExecutedEvent

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `RuleId` | `Guid` | Unique identifier of the rule |
| `RuleDescription` | `string` | Human-readable description of the rule |
| `IsActive` | `bool` | Whether the rule was active (`false` means it was skipped) |
| `Success` | `bool` | Whether the rule passed evaluation (`true` = success or skipped) |
| `ElapsedMilliseconds` | `double` | Execution time in milliseconds |
| `Exception` | `Exception?` | Optional exception if execution failed |
| `Timestamp` | `DateTime` | UTC timestamp when execution occurred (defaults to `DateTime.UtcNow`) |

### Example

```csharp
var eventData = new RuleExecutedEvent
{
    RuleId = rule.Id,
    RuleDescription = rule.Description,
    IsActive = rule.IsActive,
    Success = result.Success,
    ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
    Exception = result.Exception
};
```

---

## RuleLoggingExtensions

### Event IDs

| ID | Name | Trigger |
|----|------|---------|
| `1001` | `RuleSkipped` | `IsActive == false` |
| `1002` | `RulePassed` | `Success == true` |
| `1003` | `RuleFailed` | `Success == false` |
| `1004` | `RuleError` | `Exception != null` |

### Methods

#### `LogRuleExecuted(this ILogger, RuleExecutedEvent)`

Logs a `RuleExecutedEvent` at **Debug** level (or Error if an exception occurred).

```csharp
logger.LogRuleExecuted(eventData);
```

**Log output by state:**

| State | Level | Format |
|-------|-------|--------|
| Inactive | Debug | `[SKIP] {RuleDescription} (Id: {RuleId}) — inactive rule skipped` |
| Exception | Error | `[ERROR] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds}ms — {ErrorMessage}` |
| Success | Debug | `[PASS] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds}ms` |
| Failure | Debug | `[FAIL] {RuleDescription} (Id: {RuleId}) — {ElapsedMilliseconds}ms` |

#### `LogRuleExecutedInfo(this ILogger, RuleExecutedEvent)`

Same as above but logs at **Information** level (visible by default in most log configs).

```csharp
logger.LogRuleExecutedInfo(eventData);
```

Use this when you want rule results in standard logs, not just Debug output.

### Usage Example

```csharp
using Microsoft.Extensions.Logging;
using RoslynRules.Models;

public class AuditService
{
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public void LogRuleResult(Rule rule, RuleResult result, Stopwatch sw)
    {
        var evt = new RuleExecutedEvent
        {
            RuleId = rule.Id,
            RuleDescription = rule.Description,
            IsActive = rule.IsActive,
            Success = result.Success,
            ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
            Exception = result.Exception
        };

        // Debug-level logging (production: filtered out by default)
        _logger.LogRuleExecuted(evt);

        // Or: Information-level for audit trails
        _logger.LogRuleExecutedInfo(evt);
    }
}
```

---

## Related

- [Rule](rule.md) — Individual rule API
- [RuleResult](ruleresult.md) — Execution result structure
- [Lifecycle Events](lifecycle-events.md) — `OnRuleExecuting` / `OnRuleExecuted` hooks
- [Rule Metrics](rule-metrics.md) — Performance and execution metrics
