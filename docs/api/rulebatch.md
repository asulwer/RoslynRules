---
layout: default
title: RuleBatch
parent: API Reference
nav_order: 13
---

[← Back to API Reference](api-reference.md)

# RuleBatch

Batch evaluation for 10+ rules with shared compilation context.

```csharp
public sealed class RuleBatch : IRuleEngine
```

---

## Methods

### `AddRule(Rule)` / `AddRules(IEnumerable<Rule>)`

Adds rules to the batch.

```csharp
var batch = new RuleBatch()
    .AddRule(new Rule { Expression = "x > 0", Description = "Positive" })
    .AddRule(new Rule { Expression = "x < 100", Description = "Under limit" })
    .AddRules(GetRulesFromDatabase());
```

### `Compile(RuleParameter[], string[]?)`

Single compile pass for all rules.

```csharp
var compileParams = new[] { new RuleParameter("customer", typeof(Customer)) };
batch.Compile(compileParams);
```

### `Evaluate(RuleParameter[])` / `EvaluateParallel(RuleParameter[])`

Evaluates all rules.

```csharp
var results = batch.EvaluateParallel(parameters);
foreach (var result in results)
{
    Console.WriteLine($"{result.RuleDescription}: {(result.Success ? "PASS" : "FAIL")}");
}
```

---

## Loading Rules

| Source | Method |
|--------|--------|
| Manual | `batch.AddRule(new Rule { ... })` |
| List | `batch.AddRules(existingRules)` |
| JSON | `batch.AddRules(JsonRuleLoader.LoadWorkflowFromFile("rules.json").Rules)` |
| Database | `batch.AddRules(dbContext.Rules.Where(r => r.IsActive).ToList())` |

---

## Related

- [IRuleEngine](iruleengine.md) — Interface
- [Workflow](workflow.md) — Alternative container
