---
layout: default
title: Rule Action Chaining
parent: Examples
nav_order: 5
---

[← Back to Examples Index](index.md)

# Rule Action Chaining

Connect rules so the output of one rule feeds into another. Use `DependsOnRuleId` to declare data-flow dependencies between independent rules.

## Table of Contents
- [Parent-Child vs DependsOn](#parent-child-vs-dependson)
- [Basic Dependency](#basic-dependency)
- [Multi-Stage Pipeline](#multi-stage-pipeline)
- [Accessing Dependency Results](#accessing-dependency-results)
- [Parallel Execution with Dependencies](#parallel-execution-with-dependencies)
- [Validation](#validation)
- [Common Patterns](#common-patterns)

## Parent-Child vs DependsOn

Rules engine supports two types of relationships between rules. Understanding the difference is critical for correct rule design.

### Parent-Child (Structural Nesting)

A parent rule *contains* child rules. Children are evaluated as part of the parent's evaluation. The parent succeeds only if all active children succeed.

**Use for:** Sub-conditions that compose a larger check.

```csharp
var adultCheck = new Rule
{
    Description = "Valid adult customer",
    Expression = "customer.IsAdult && customer.HasName",
    IsActive = true
};

adultCheck.ChildRules.Add(new Rule
{
    Description = "Age check",
    Expression = "customer.Age >= 18"
});

adultCheck.ChildRules.Add(new Rule
{
    Description = "Name check",
    Expression = "!string.IsNullOrEmpty(customer.Name)"
});
```

**Execution order:**
1. Age check (child)
2. Name check (child)
3. Valid adult customer (parent expression)

**Failure behavior:** If Age check fails, adultCheck fails immediately. Name check still runs (all children are evaluated).

### DependsOn (Data-Flow Dependency)

Rule B *depends on* Rule A's output. Rule B can read Rule A's result and makes its own decision. The rules remain independent — Rule A doesn't know about Rule B.

**Use for:** Multi-stage pipelines where later stages need earlier outputs.

```csharp
var validateCustomer = new Rule
{
    Description = "Validate customer",
    Expression = "customer != null && customer.IsActive",
    IsActive = true
};

var checkCredit = new Rule
{
    Description = "Check credit",
    DependsOnRuleId = validateCustomer.Id,
    Expression = "customer.CreditScore >= 700",
    IsActive = true
};
```

**Execution order:**
1. Validate customer
2. Check credit (only after validate customer completes)

**Failure behavior:** If validate customer fails, check credit still runs. The upstream rule's `Action` can mutate the shared parameter object, and check credit reads those mutated properties in its `Expression`/`Action`.

### Comparison Table

| Aspect | Parent-Child | DependsOn |
|--------|-------------|-----------|
| Relationship | Structural containment | Data-flow dependency |
| Direction | Parent knows children | Dependent knows dependency |
| Execution | Children first, then parent | Dependency first, then dependent |
| Failure impact | Parent fails if any child fails | Dependent still runs regardless |
| Access to results | Parent cannot read child values | Dependent can read dependency values |
| Use case | Composite conditions | Multi-stage pipelines |
| Cycle detection | Child tree validation | Dependency graph validation |

## Basic Dependency

Create two rules where the second depends on the first.

```csharp
var ruleA = new Rule
{
    Description = "Calculate tax",
    Expression = "customer.Amount > 0",
    Action = "customer.TaxAmount = customer.Amount * 0.08",
    IsActive = true
};

var ruleB = new Rule
{
    Description = "Calculate total",
    DependsOnRuleId = ruleA.Id,
    Expression = "customer.TaxAmount > 0",
    Action = "customer.Total = customer.Amount + customer.TaxAmount",
    IsActive = true
};

var workflow = new Workflow
{
    Description = "Order calculation",
    Rules = new List<Rule> { ruleB, ruleA } // Any order
};

workflow.Validate();
workflow.Compile(parameters);

var results = workflow.Execute(parameters).ToList();
// results[0] = Calculate tax (dependency)
// results[1] = Calculate total (dependent)
```

## Multi-Stage Pipeline

Build a processing pipeline where each stage depends on the previous.

```csharp
// Stage 1: Validate input
var validateInput = new Rule
{
    Description = "Validate input",
    Expression = "customer != null && customer.Id > 0",
    IsActive = true
};

// Stage 2: Enrich data (fetch additional info)
var enrichData = new Rule
{
    Description = "Enrich data",
    DependsOnRuleId = validateInput.Id,
    Expression = "true",
    Action = "customer.CreditScore = CreditService.GetScore(customer.Id)",
    IsActive = true
};

// Stage 3: Evaluate risk
var evaluateRisk = new Rule
{
    Description = "Evaluate risk",
    DependsOnRuleId = enrichData.Id,
    Expression = "customer.CreditScore >= 600",
    IsActive = true
};

// Stage 4: Approve or reject
var finalDecision = new Rule
{
    Description = "Final decision",
    DependsOnRuleId = evaluateRisk.Id,
    Expression = "customer.CreditScore >= 600",
    Action = "customer.Approved = true",
    IsActive = true
};

var workflow = new Workflow
{
    Description = "Loan approval pipeline",
    Rules = new List<Rule> { finalDecision, evaluateRisk, enrichData, validateInput }
};

workflow.Validate();
workflow.Compile(parameters);

// Execute: Validate → Enrich → Evaluate → Decide
var results = workflow.Execute(parameters).ToList();
```

## Accessing Dependency Results

To pass data between dependent rules, have the upstream rule's `Action` mutate the shared parameter object, and have the downstream rule read those mutated properties in its `Expression`/`Action`. Compiled expressions and actions can only reference the declared `RuleParameter` objects — there is no `context` variable in scope inside an expression string.

`RuleContext` is a host-side (C#) API for inspecting results after execution. Its `GetResult`/`GetValue`/`TryGetValue` methods can only be called from your own C# code, never from inside an expression or action string.

### RuleContext API (host-side only)

```csharp
public class RuleContext
{
    // Store a result
    void StoreResult(Guid ruleId, RuleResult result);
    
    // Retrieve a result
    RuleResult? GetResult(Guid ruleId);
    
    // Check if a rule has been executed
    bool HasResult(Guid ruleId);
    
    // Get typed value from a result (returns default(T) if not found/failed)
    T? GetValue<T>(Guid ruleId);
    
    // Get typed value safely — returns true only if rule succeeded and value is of type T
    bool TryGetValue<T>(Guid ruleId, out T? value);
    
    // Clear all results
    void Clear();
}
```

### Passing Values Between Rules

The upstream rule mutates a property on the shared parameter object; the downstream rule reads it.

```csharp
var calculateTax = new Rule
{
    Description = "Calculate tax",
    Expression = "customer.Amount > 0",
    Action = "customer.TaxAmount = customer.Amount * 0.08m",
    IsActive = true
};

var calculateTotal = new Rule
{
    Description = "Calculate total",
    DependsOnRuleId = calculateTax.Id,
    Expression = "customer.TaxAmount > 0",
    Action = "customer.Total = customer.Amount + customer.TaxAmount",
    IsActive = true
};
```

### Guarding Against Unset Values

If the upstream rule may not have set a value, guard the downstream read against the property's default.

```csharp
var calculateTotal = new Rule
{
    Description = "Calculate total",
    DependsOnRuleId = calculateTax.Id,
    Expression = "true",
    Action = @"customer.Total = customer.TaxAmount > 0
        ? customer.Amount + customer.TaxAmount
        : customer.Amount;",
    IsActive = true
};
```

### Conditional Logic Based on Dependencies

The upstream rule records its outcome on the shared object; the downstream rule reads it.

```csharp
var checkAuthentication = new Rule
{
    Description = "Check authentication",
    Expression = "customer.IsAuthenticated",
    Action = "customer.IsAuthenticated = true",
    IsActive = true
};

var checkAuthorization = new Rule
{
    Description = "Check authorization",
    DependsOnRuleId = checkAuthentication.Id,
    Expression = @"customer.IsAuthenticated && customer.HasAdminRole",
    IsActive = true
};
```

## Parallel Execution with Dependencies

When using `ExecuteParallelAsync`, independent rules run concurrently while dependency chains run sequentially.

```csharp
var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };
var ruleC = new Rule { Description = "C", Expression = "true", IsActive = true }; // Independent
var ruleD = new Rule { Description = "D", DependsOnRuleId = ruleB.Id, Expression = "true", IsActive = true };

var workflow = new Workflow
{
    Rules = new List<Rule> { ruleA, ruleB, ruleC, ruleD }
};

workflow.Validate();
workflow.Compile(parameters);

// Execution:
// Phase 1 (parallel): ruleA, ruleC
// Phase 2 (parallel): ruleB (after A completes), ruleC already done
// Phase 3 (parallel): ruleD (after B completes)
var results = await workflow.ExecuteParallelAsync(parameters);
```

**Performance tip:** Design your rules to maximize independent rules for better parallelization.

## Validation

`Validate()` performs comprehensive dependency checking.

### Missing Dependency Detection

```csharp
var rule = new Rule
{
    Description = "Invalid rule",
    DependsOnRuleId = Guid.NewGuid(), // Non-existent rule
    Expression = "true",
    IsActive = true
};

var workflow = new Workflow { Rules = new List<Rule> { rule } };

// Throws: RuleValidationException
// "Rule 'Invalid rule' (Id: ...) depends on rule ... which does not exist or is inactive."
workflow.Validate();
```

### Circular Dependency Detection

```csharp
var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };

// Create cycle: A depends on B, B depends on A
ruleA.DependsOnRuleId = ruleB.Id;

var workflow = new Workflow { Rules = new List<Rule> { ruleA, ruleB } };

// Throws: CircularReferenceException
// "Dependency chain on rule 'A'"
workflow.Validate();
```

## Common Patterns

### Pattern 1: Gatekeeper Pattern

Use a dependency as a gatekeeper. The dependent rule always runs but checks the gatekeeper's result.

```csharp
var gatekeeper = new Rule
{
    Description = "Feature enabled",
    Expression = "FeatureFlags.IsEnabled(\"PremiumFeature\")",
    Action = "customer.FeatureEnabled = FeatureFlags.IsEnabled(\"PremiumFeature\")",
    IsActive = true
};

var premiumCheck = new Rule
{
    Description = "Premium validation",
    DependsOnRuleId = gatekeeper.Id,
    Expression = "customer.FeatureEnabled && customer.IsPremium",
    IsActive = true
};
```

### Pattern 2: Data Enrichment Pipeline

Each stage adds data to the parameter object for subsequent stages.

```csharp
var fetchProfile = new Rule
{
    Description = "Fetch profile",
    Expression = "true",
    Action = "customer.Profile = ProfileService.Get(customer.Id)",
    IsActive = true
};

var validateProfile = new Rule
{
    Description = "Validate profile",
    DependsOnRuleId = fetchProfile.Id,
    Expression = "customer.Profile != null && customer.Profile.IsComplete",
    IsActive = true
};

var processOrder = new Rule
{
    Description = "Process order",
    DependsOnRuleId = validateProfile.Id,
    Expression = "true",
    Action = "OrderService.Process(customer)",
    IsActive = true
};
```

### Pattern 3: Branching Logic

One dependency, multiple dependents that make different decisions.

```csharp
var eligibilityCheck = new Rule
{
    Description = "Eligibility",
    Expression = "customer.Age >= 18 && customer.Income > 30000",
    Action = "customer.IsEligible = customer.Age >= 18 && customer.Income > 30000",
    IsActive = true
};

var standardOffer = new Rule
{
    Description = "Standard offer",
    DependsOnRuleId = eligibilityCheck.Id,
    Expression = "customer.IsEligible && customer.Income < 100000",
    IsActive = true
};

var premiumOffer = new Rule
{
    Description = "Premium offer",
    DependsOnRuleId = eligibilityCheck.Id,
    Expression = "customer.IsEligible && customer.Income >= 100000",
    IsActive = true
};
```

### Pattern 4: Error Handling Chain

Propagate errors through the dependency chain.

```csharp
var parseInput = new Rule
{
    Description = "Parse input",
    Expression = "int.TryParse(customer.AgeInput, out var age)",
    Action = "customer.ParsedAge = age",
    IsActive = true
};

var validateAge = new Rule
{
    Description = "Validate age",
    DependsOnRuleId = parseInput.Id,
    Expression = @"customer.ParsedAge >= 18",
    Action = "customer.IsAdult = customer.ParsedAge >= 18",
    IsActive = true
};

var processAdult = new Rule
{
    Description = "Process adult",
    DependsOnRuleId = validateAge.Id,
    Expression = @"customer.IsAdult",
    Action = "customer.Processed = true",
    IsActive = true
};
```

## Troubleshooting

### "Rule depends on rule ... which does not exist"

**Cause:** `DependsOnRuleId` points to a rule not in the workflow, or the dependency is inactive.

**Fix:** Ensure the dependency rule is added to the workflow and `IsActive = true`.

### "Dependency cycle detected"

**Cause:** Circular dependency chain (A → B → C → A).

**Fix:** Redesign rules to break the cycle. Use parent-child relationships for tight coupling instead.

### Downstream rule reads a stale or default value

**Cause:** The downstream rule ran before the upstream rule mutated the shared parameter object, or the upstream `Action` never set the property.

**Fix:** Ensure `DependsOnRuleId` is set correctly so the dependency runs first, and confirm the upstream rule's `Action` assigns the property the downstream rule reads. Remember: compiled expressions/actions can only reference the declared parameters — there is no `context` available inside expression strings. Use the host-side `RuleContext` only from C# code after execution.

### Priority not respected

**Cause:** Dependency ordering overrides priority.

**Fix:** This is by design. Dependencies always execute first. Priority only affects ordering within the same dependency level.

## See Also

- [Parent-Child Rules](#parent-child-vs-dependson) — Bottom-up evaluation patterns (see section above)
- [Rule Testing Framework](testing-framework.md) — Testing chained rules
- [API Reference: RuleContext](../api-reference.md#rulecontext)
