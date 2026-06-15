---
layout: default
title: Migration
nav_order: 5
---

[<- Back to Documentation Index](index.md)

# Migration

## From 0.x to 1.x

### Breaking Changes

| 0.x | 1.x | Migration |
|-----|-----|-----------|
| `Rule.Id` auto-generated | `Rule.Id` still auto-generated | No change |
| Single parameter only | Multi-parameter support | Remove wrapper structs |
| `workflow.Rules` mutable after compile | Immutable after `Compile()` | Add rules before compile |
| No `ValidateSemantics` | Static `ValidateSemantics` overloads | Use for input validation |
| `AssemblyReferenceProvider` basic | Whitelist + hardening | Review whitelist |

### Multi-Parameter Migration

Before (0.x):
```csharp
// Wrap in struct
public record Input(int A, int B);
var param = new RuleParameter("input", typeof(Input), new Input(1, 2));
var rule = new Rule { Expression = "input.A > input.B" };
```

After (1.x):
```csharp
// Direct multi-parameter
var parameters = new[] {
    new RuleParameter("a", typeof(int), 1),
    new RuleParameter("b", typeof(int), 2)
};
var rule = new Rule { Expression = "a > b" };
```

### Workflow Immutability

Before (0.x):
```csharp
workflow.Compile(parameters);
workflow.Rules.Add(newRule);  // Was allowed
workflow.Compile(parameters); // Re-compile
```

After (1.x):
```csharp
workflow.Rules.Add(newRule);
workflow.Compile(parameters);  // Locks Rules
// workflow.Rules.Add(another); // NotSupportedException
```

### Static ValidateSemantics

New in 1.x — validate user input without creating a Rule:

```csharp
// Validate API input before storing
Rule.ValidateSemantics(userExpression, typeof(Customer), "customer");
```

---

## From Microsoft.RulesEngine

### Key Changes

| RulesEngine | This Engine |
|-------------|-------------|
| `new RulesEngine.RulesEngine()` | `new Workflow()` |
| `engine.ExecuteAllRulesAsync()` | `workflow.Execute()` or `workflow.ExecuteParallelAsync()` |
| `Rule.Expression` | `Rule.Expression` (same) |
| `Rule.RuleName` | `Rule.Description` |
| `Rule.SuccessEvent` | Use `Rule.Action` |
| `Rule.ErrorMessage` | Not needed — exceptions on failure |
| Multi-parameter | ✅ Supported directly (up to 16) |
| `DynamicInvoke` | Typed delegates |

## Example Migration

### Before (RulesEngine)

```csharp
var rules = new[] {
    new Rule {
        RuleName = "CheckAge",
        Expression = "input1.Age >= 18"
    }
};

var engine = new RulesEngine.RulesEngine(rules);
var result = await engine.ExecuteAllRulesAsync("CheckAge", customer);
```

### After (This Engine)

```csharp
var rule = new Rule {
    Description = "CheckAge",
    Expression = "customer.Age >= 18"
};

var workflow = new Workflow {
    Rules = new List<Rule> { rule }
};

var compileParam = new RuleParameter("customer", typeof(Customer));
var executeParam = new RuleParameter("customer", typeof(Customer), customer);

workflow.Validate();
workflow.Compile(new[] { compileParam });

var results = workflow.Execute(new[] { executeParam });
```

**Tip:** You can compile without values if you separate compilation from execution:

```csharp
// Compile at startup with null values — types are what matter
workflow.Compile(new[]
{
    new RuleParameter("customer", typeof(Customer))  // value defaults to null
});

// Execute later with real instances
var results = workflow.Execute(new[]
{
    new RuleParameter("customer", typeof(Customer), customer)
});
```

## Breaking Changes

1. **No built-in error message** — Handle exceptions in caller
2. **Expression uses parameter name** — Not `input1`, use declared name
3. **Compile once** — Call `Compile()` before executing

## Benefits of Migrating

| Metric | Improvement |
|--------|-------------|
| Execution speed | 10-100x faster (no System.Linq.Dynamic.Core) |
| Memory | Lower allocation |
| Thread safety | Immutable rules, no locks |
| Validation | Catch errors before runtime |
| Async | Native async/await support |
| Multi-parameter | Up to 16 parameters directly |

## License

MIT License — same permissive terms as before.
