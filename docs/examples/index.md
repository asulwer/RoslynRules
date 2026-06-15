---
layout: default
title: Examples
nav_order: 4
has_children: true
---

[← Back to Documentation Index](../index.md)

# Examples

Quick-reference code snippets for common scenarios.

## On This Page
- [Basic Boolean Rule](#basic-boolean-rule)
- [Rule with Action](#rule-with-action)
- [Parent with Child Rules](#parent-with-child-rules)
- [Multiple Parameters](#multiple-parameters)
- [Async Rule](#async-rule)
- [Returning Multiple Values](#returning-multiple-values)
- [Workflow with Multiple Rules](#workflow-with-multiple-rules)
- [Dependency Chaining](#dependency-chaining)
- [Logging with Serilog](#logging-with-serilog)
- [ExpandoObject](#expandoobject-dynamic)
- [JSON Configuration](#json-configuration)
- [Loading Rules into a Batch](#loading-rules-into-a-batch)
- [Batch Evaluation](#batch-evaluation-10-rules)
- [Rule Priority](#rule-priority)
- [Per-Rule Timeout](#per-rule-timeout)
- [Validation Before Compile](#validation-before-compile)
- [Semantic Validation (No Rule Instance)](#semantic-validation-no-rule-instance)
- [Non-Throwing Validation](#non-throwing-validation)

## Detailed Guides

| Guide | What It Covers |
|-------|---------------|
| [Rule Action Chaining](rule-action-chaining.md) | Parent-Child vs DependsOn, multi-stage pipelines, RuleContext |
| [Streaming and Cancellation](streaming-and-cancellation.md) | ExecuteAsync, ExecuteBufferedAsync, CancellationToken patterns |
| [Testing Framework](testing-framework.md) | RuleTest, RuleResult assertions, test suites, custom assertions |
| [Real-World Use Cases](real-world-use-cases.md) | Form validation, fraud detection, feature flags, compliance, pricing |
| [EF Core Serialization](ef-serialization.md) | DbContext setup, storing/loading rules, temporal tables |
| [When to Use What](when-to-use-what.md) | Decision matrix, execution modes, choosing by rule count |
| [Localization](localization.md) | i18n with DescriptionKey and IRuleDescriptionProvider |
| [Visualization](visualization.md) | DOT/Mermaid dependency graphs |

## Basic Boolean Rule

```csharp
var rule = new Rule
{
    Description = "Adult check",
    Expression = "customer.Age >= 18",
    IsActive = true
};
```

## Rule with Action

```csharp
var rule = new Rule
{
    Description = "Mark adult",
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true",
    IsActive = true
};
```

## Parent with Child Rules

Children evaluated bottom-up. Parent only runs if all children pass.

```csharp
var parent = new Rule
{
    Description = "Full validation",
    Expression = "customer.IsValid"
};

var child1 = new Rule
{
    Description = "Name check",
    Expression = "!string.IsNullOrEmpty(customer.Name)"
};

var child2 = new Rule
{
    Description = "Age check",
    Expression = "customer.Age > 0"
};

parent.ChildRules.Add(child1);
parent.ChildRules.Add(child2);
```

## Multiple Parameters

Pass multiple parameters directly — up to 16.

```csharp
var rule = new Rule
{
    Description = "Price check",
    Expression = "price > 0 && quantity > 0",
    IsActive = true
};

var parameters = new[]
{
    new RuleParameter("price", typeof(decimal), 9.99m),
    new RuleParameter("quantity", typeof(int), 5)
};

var compiler = new ExpressionCompiler();
rule.Compile(compiler, parameters);

var result = rule.Execute(parameters);
// result.Success = true
```

**Parameter names in expressions** match exactly:

```csharp
var rule = new Rule
{
    Expression = "x > y"
};

var parameters = new[]
{
    new RuleParameter("x", typeof(int), 10),
    new RuleParameter("y", typeof(int), 5)
};

rule.Compile(compiler, parameters);
rule.Execute(parameters); // Success = true
```

## Async Rule

Auto-detected by `await` keyword. Compiled to `Func<T, Task<bool>>`.

```csharp
var rule = new Rule
{
    Description = "Check price from API",
    Expression = "await PriceService.GetAsync(customer.ProductId) > 100"
};

// Compile normally
workflow.Compile(parameters);

// Execute async
var results = await workflow.ExecuteParallelAsync(parameters);
```

## Returning Multiple Values

Wrap outputs in a struct:

```csharp
public struct ValidationResult
{
    public bool IsAdult { get; set; }
    public bool IsPremium { get; set; }
}

var rule = new Rule
{
    Expression = "new ValidationResult { 
        IsAdult = customer.Age >= 18,
        IsPremium = customer.Age >= 65
    }"
};

// Access result.Data as ValidationResult
```

## Workflow with Multiple Rules

```csharp
var workflow = new Workflow
{
    Description = "Customer processing",
    Rules = new List<Rule>
    {
        new Rule { Expression = "customer.Age >= 18", Description = "Adult" },
        new Rule { Expression = "customer.Name.StartsWith(\"A\")", Description = "Starts with A" },
        new Rule { 
            Expression = "customer.IsActive",
            Action = "customer.Processed = true",
            Description = "Process active"
        }
    }
};

workflow.Validate();
workflow.Compile(parameters);

// All rules evaluated, results in order
var results = workflow.ExecuteParallel(parameters);
```

## Dependency Chaining

Chain rules with `DependsOnRuleId` so downstream rules access upstream results via `RuleContext`.

```csharp
var authId = Guid.NewGuid();
var authRule = new Rule(authId)
{
    Description = "Authenticated?",
    Expression = "user.IsAuthenticated",
    IsActive = true
};

var adminId = Guid.NewGuid();
var adminRule = new Rule(adminId)
{
    Description = "Admin?",
    Expression = "context.GetValue<bool>(authId) && user.Role == \"Admin\"",
    DependsOnRuleId = authId,
    IsActive = true
};

var discountId = Guid.NewGuid();
var discountRule = new Rule(discountId)
{
    Description = "VIP discount",
    Expression = "context.GetValue<bool>(adminId) && user.IsVip",
    DependsOnRuleId = adminId,
    IsActive = true
};

var workflow = new Workflow();
workflow.Rules.Add(authRule);
workflow.Rules.Add(adminRule);
workflow.Rules.Add(discountRule);

workflow.Compile(new[] { new RuleParameter("user", typeof(User)) });

// Auth → Admin → Discount (dependency order)
var results = workflow.Execute(new[]
{
    new RuleParameter("user", typeof(User), currentUser)
});
```

**Key points:**
- `DependsOnRuleId` ensures execution order
- `RuleContext.GetValue<T>(ruleId)` retrieves upstream results
- Cyclic dependencies throw `CircularReferenceException` on `Validate()`

## Logging with Serilog

```csharp
using Serilog;
using Microsoft.Extensions.Logging;

// Create a Serilog logger
var serilog = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var logger = new SerilogLoggerFactory(serilog).CreateLogger<Rule>();

// Attach to any rule
var rule = new Rule
{
    Description = "Check adult",
    Expression = "customer.Age >= 18",
    Logger = logger
};

// Execute — output appears in console:
// [15:42:10.123] [PASS] Check adult (Id: ...) — 0.031ms
```

## ExpandoObject (Dynamic)

Use when the data shape is not known at compile time. Slower but flexible.

```csharp
using System.Dynamic;

dynamic customer = new ExpandoObject();
customer.Name = "Alice";
customer.Age = 25;

var parameters = new[]
{
    new RuleParameter("customer", typeof(object), customer)
};

var rule = new Rule
{
    Description = "Check adult",
    Expression = "((dynamic)customer).Age >= 18",
    IsActive = true
};

var compiler = new ExpressionCompiler();
rule.Compile(compiler, parameters, new[] { "System.Dynamic" });

var result = rule.Execute(parameters);
// result.Success = true
```

**Caveat:** Missing properties return `null`, not throw. Test for existence if needed:
```csharp
Expression = "((dynamic)customer).Age != null && ((dynamic)customer).Age >= 18"
```

## JSON Configuration

Store rules in JSON files for configuration-driven setups:

```csharp
using RoslynRules.Extensions;

// Load from JSON
var workflow = JsonRuleLoader.LoadWorkflowFromFile("customer-rules.json");

// Validate, compile, execute as normal
workflow.Validate();
workflow.Compile(parameters);
var results = workflow.Execute(parameters);
```

**customer-rules.json:**
```json
{
  "description": "Customer validation",
  "rules": [
    {
      "description": "Adult check",
      "expression": "customer.Age >= 18",
      "isActive": true
    },
    {
      "description": "Name required",
      "expression": "!string.IsNullOrEmpty(customer.Name)",
      "isActive": true
    }
  ]
}
```

## Loading Rules into a Batch

Combine rules from multiple sources into a single batch:

```csharp
using RoslynRules.Batch;
using RoslynRules.Extensions;

var batch = new RuleBatch();

// From code
batch.AddRule(new Rule
{
    Description = "Adult check",
    Expression = "customer.Age >= 18"
});

// From JSON file
var jsonWorkflow = JsonRuleLoader.LoadFromFile("compliance-rules.json");
batch.AddRules(jsonWorkflow.Rules);

// From database (EF Core)
var dbRules = dbContext.Rules
    .Where(r => r.WorkflowId == workflowId && r.IsActive)
    .ToList();
batch.AddRules(dbRules);

// From a service
var apiRules = await ruleService.GetRulesForTenantAsync(tenantId);
batch.AddRules(apiRules);

// Single compile, parallel evaluation
batch.Compile(parameters);
var results = batch.EvaluateParallel(parameters);

foreach (var result in results.Where(r => !r.Success))
{
    Console.WriteLine($"Failed: {result.RuleDescription}");
}
```

## Batch Evaluation (10+ Rules)

Evaluate many rules together with shared compilation:

```csharp
var batch = new RuleBatch();

// Add 10 validation rules
for (int i = 0; i < 10; i++)
{
    batch.AddRule(new Rule
    {
        Description = $"Check {i + 1}",
        Expression = $"customer.Field{i} > 0"
    });
}

// Compile once
batch.Compile(parameters);

// Evaluate all at once — parallel mode for speed
var results = batch.EvaluateParallel(parameters);

// Summary
var passCount = results.Count(r => r.Success);
Console.WriteLine($"Passed: {passCount}/{results.Length}");
```

## Rule Priority

Control execution order when some checks must run before others:

```csharp
var workflow = new Workflow
{
    Rules =
    {
        // Critical checks run first
        new Rule
        {
            Description = "Check authentication",
            Expression = "customer.IsAuthenticated",
            Priority = 100
        },
        new Rule
        {
            Description = "Check authorization",
            Expression = "customer.HasPermission",
            Priority = 90
        },
        // Standard validation
        new Rule
        {
            Description = "Validate email",
            Expression = "customer.Email.Contains(\"@\")",
            Priority = 0 // Default
        },
        // Cleanup / logging runs last
        new Rule
        {
            Description = "Audit log",
            Expression = "true",
            Action = "Log(customer.Id)",
            Priority = -10
        }
    }
};

workflow.Compile(parameters);

// Order: Auth (100) → AuthZ (90) → Email (0) → Audit (-10)
var results = workflow.Execute(parameters);
```

**Priority rules:**
- Higher number = earlier execution
- `0` is default
- Negative numbers = after defaults
- Same priority = original order preserved
- Immutable after `Compile()`

## Per-Rule Timeout

Prevent infinite loops or blocking operations from hanging execution:

```csharp
var rule = new Rule
{
    Description = "External API call",
    Expression = "await CheckApiAsync(customer.Id)",
    Timeout = TimeSpan.FromSeconds(10),  // Fail after 10 seconds
    IsActive = true
};

// If execution exceeds 10s, RuleTimeoutException is thrown
```

## Validation Before Compile

```csharp
try
{
    workflow.Validate();
    workflow.Compile(parameters);
}
catch (InvalidOperationException ex)
{
    // Catches:
    // - Empty rules
    // - Syntax errors in expressions
    // - Circular child references
    // - Duplicate rule IDs
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## Semantic Validation (No Rule Instance)

Validate an expression string without creating a Rule — useful for validating user input or API parameters before storing them.

```csharp
// Using a Type
Rule.ValidateSemantics("param > 0", typeof(int));

// Using a type alias
Rule.ValidateSemantics("name.Length > 0", "string", "name");

// Using full type name
Rule.ValidateSemantics("date.Year >= 2000", "System.DateTime", "date");
```

**Supported aliases:** `bool`, `byte`, `char`, `decimal`, `double`, `float`, `int`, `long`, `short`, `string`, `object`, and any full type name (e.g., `System.DateTime`).

**Throws `RuleCompilationException`** if the expression has undefined variables, missing types, or incorrect method signatures.

## Non-Throwing Validation

Collect all validation errors without throwing:

```csharp
ValidationError[] errors = workflow.ValidateAll();
foreach (var error in errors)
{
    Console.WriteLine($"[{error.ErrorType}] {error.Message}");
}
// ErrorType values: NoActiveRules, EmptyRule, SyntaxError, CircularReference, DuplicateRuleId, MissingDependency
```
