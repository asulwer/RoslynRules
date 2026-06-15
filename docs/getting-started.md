---
layout: default
title: Getting Started
nav_order: 2
---

[← Back to Documentation Index](index.md)

# Getting Started

## Installation

```bash
dotnet add package RoslynRules --version 1.0.0
```

Or reference the project directly:

```xml
<ProjectReference Include="..\RoslynRules\RoslynRules.csproj" />
```

## Your First Rule

### 1. Define a Model

```csharp
public class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool IsAdult { get; set; }
}
```

### 2. Create a Rule

```csharp
using RoslynRules.Models;

var adultRule = new Rule
{
    Description = "Check if customer is an adult",
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true",
    IsActive = true
};
```

### 3. Create a Workflow

```csharp
var workflow = new Workflow
{
    Description = "Customer validation",
    Rules = new List<Rule> { adultRule }
};
```

### 4. Define Parameters

For **compilation**, only the parameter **type** and **name** are needed.

```csharp
var compileParams = new[]
{
    RuleParameter.ForCompile("customer", typeof(Customer))
};
```

For **execution**, pass a parameter with a real value:

```csharp
var customer = new Customer { Name = "Alice", Age = 25 };

var executeParams = new[]
{
    RuleParameter.ForExecute("customer", typeof(Customer), customer)
};
```

### Multiple Parameters

Rules can accept multiple parameters directly — up to 16.

```csharp
var rule = new Rule
{
    Description = "Price validation",
    Expression = "price > 0 && quantity > 0"
};

var parameters = new[]
{
    RuleParameter.ForCompile("price", typeof(decimal)),
    RuleParameter.ForCompile("quantity", typeof(int))
};

rule.Compile(compiler, parameters);
```

### 5. Validate, Compile, Execute

```csharp
// Catch errors before compiling
workflow.Validate();

// Compile once — types and names only
workflow.Compile(compileParams);

// Execute many times with different values
var results = workflow.Execute(executeParams);

foreach (var result in results)
{
    Console.WriteLine($"Success: {result.Success}");
}
```

**Key point:** Compile with `ForCompile()` (types only). Execute with `ForExecute()` (real values). Separate compilation from execution.

## Next Steps

- [Learn about child rules and bottom-up evaluation](api-reference.md#rule)
- [See async expression examples](examples/streaming-and-cancellation.md)
- [Compare performance modes](performance.md)
