---
layout: default
title: Testing Framework
parent: Examples
nav_order: 7
---

[← Back to Examples Index](index.md)

# Testing Framework

Built-in assertions for testing rules without external test libraries. Works with any test runner (xUnit, NUnit, MSTest).

## Table of Contents
- [Basic Rule Test](#basic-rule-test)
- [RuleResult Assertions](#ruleresult-assertions)
- [Test Suites](#test-suites)
- [Custom Assertions](#custom-assertions)
- [Workflow Testing](#workflow-testing)
- [Integration with FluentAssertions](#integration-with-fluentassertions)

## Basic Rule Test

Create a `RuleTest` to declaratively specify inputs and expected outputs.

```csharp
using RoslynRules.Testing;

var test = RuleTest.For(adultRule)
    .WithInput("customer", new Customer { Age = 25, Name = "Alice" })
    .ExpectSuccess()
    .ExpectAllChildrenPass()
    .ExpectValue(true);

// Run() throws RuleAssertionException if any expectation fails,
// otherwise returns the RuleResult.
var result = test.Run();
// result.Success = true
// result.Exception = null
```

### Testing Failure Cases

```csharp
var test = RuleTest.For(adultRule)
    .WithInput("customer", new Customer { Age = 16, Name = "Bob" })
    .ExpectFailure();

var result = test.Run();
// result.Success = false
// result.Exception = null
```

### Testing with Multiple Inputs

```csharp
var test = RuleTest.For(complexRule)
    .WithInput("customer", new Customer { Age = 25 })
    .WithInput("config", new Config { MinAge = 18 })
    // Note: Rules only supports one parameter at runtime
    // Wrap multiple values in a struct/class
    .ExpectSuccess();
```

## RuleResult Assertions

Use fluent assertions directly on `RuleResult` for concise test code.

### Success and Failure

```csharp
var result = rule.Execute(parameters);

// Assert success
result.ShouldPass();

// Assert failure
result.ShouldFail();

// Assert inactive (skipped)
result.ShouldBeInactive();
```

### Value Assertions

```csharp
var result = rule.Execute(parameters);

// Assert specific value
result.ShouldHaveValue(42);

// Assert type
result.ShouldHaveValueOfType<int>();
result.ShouldHaveValueOfType<ValidationResult>();
```

### Child Rule Assertions

```csharp
var parent = new Rule
{
    Description = "Parent",
    Expression = "true",
    ChildRules = new List<Rule>
    {
        new Rule { Description = "Child 1", Expression = "true" },
        new Rule { Description = "Child 2", Expression = "false" }
    }
};

var compiler = new ExpressionCompiler();
parent.Compile(compiler, parameters);
var result = parent.Execute(parameters);

// All children passed
result.ShouldHaveAllChildrenPass();

// At least one child failed
result.ShouldHaveChildFailure();

// Specific number of children
result.ShouldHaveChildCount(2);

// Find child by description and assert
result.ShouldHaveChild("Child 1").ShouldPass();
result.ShouldHaveChild("Child 2").ShouldFail();
```

### Exception Assertions

```csharp
var badRule = new Rule
{
    Description = "Bad rule",
    Expression = "throw new ArgumentException(\"test\")"
};

var compiler = new ExpressionCompiler();
badRule.Compile(compiler, parameters);
var result = badRule.Execute(parameters);

// Assert exception type
result.ShouldHaveThrown<ArgumentException>();

// Assert no exception
result.ShouldNotHaveThrown();
```

### Workflow Result Assertions

```csharp
var results = workflow.Execute(parameters);

// All passed
results.ShouldAllPass();

// At least one failed
results.ShouldHaveAnyFailure();

// Find specific rule and assert
results.ShouldContainRule("Adult check").ShouldPass();
results.ShouldContainRule("Name check").ShouldFail();
```

## Test Suites

Group multiple tests into a suite for batch execution and reporting.

### Creating a Suite

```csharp
var suite = new RuleTestSuite()
    .AddTest(RuleTest.For(adultRule)
        .WithInput("customer", new Customer { Age = 25 })
        .ExpectSuccess()
        .ExpectValue(true))
    .AddTest(RuleTest.For(nameRule)
        .WithInput("customer", new Customer { Name = "Alice" })
        .ExpectSuccess()
        .ExpectValue(true))
    .AddTest(RuleTest.For(inactiveRule)
        .WithInput("customer", new Customer())
        .ExpectFailure());
```

### Running a Suite

```csharp
var result = suite.Run();

// Summary
Console.WriteLine(result.ToString());
// Rule Test Suite: 3 passed, 0 failed (3 total)
//   ✅ PASS Adult check
//   ✅ PASS Name check
//   ✅ PASS Inactive rule

// Detailed results
foreach (var testResult in result.Tests)
{
    Console.WriteLine($"{testResult.TestName}: {(testResult.Passed ? "PASS" : "FAIL")}");
    if (!testResult.Passed)
    {
        Console.WriteLine($"  Error: {testResult.Exception?.Message}");
    }
}
```

### Fail on Error

```csharp
// Throws RuleAssertionException if any test failed
suite.Run().ThrowOnFailure();
```

### Conditional Tests

```csharp
var suite = new RuleTestSuite();

// Only add test if feature is enabled
if (FeatureFlags.IsEnabled("PremiumValidation"))
{
    suite.AddTest(RuleTest.For(premiumRule)
        .WithInput("customer", premiumCustomer)
        .ExpectSuccess());
}

var result = suite.Run();
```

## Custom Assertions

Extend tests with custom assertion logic.

### Using Assert Method

```csharp
var test = RuleTest.For(rule)
    .WithInput("customer", new Customer { Age = 25, Name = "Alice" })
    .Assert(r =>
    {
        // Custom assertion logic
        var customer = (Customer)r.Value;
        customer.IsAdult.Should().BeTrue();
        customer.Name.Should().StartWith("A");
        
        return r; // Return RuleResult for chaining
    });
```

### Combining Multiple Assertions

```csharp
var test = RuleTest.For(rule)
    .WithInput("customer", customer)
    .ExpectSuccess()
    .Assert(r => r.ShouldHaveValueOfType<ValidationResult>())
    .Assert(r =>
    {
        var result = (ValidationResult)r.Value;
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        return r;
    });
```

## Workflow Testing

Test complete workflows with multiple rules.

### Basic Workflow Test

```csharp
var workflow = new Workflow
{
    Rules = new List<Rule>
    {
        new Rule { Description = "Age", Expression = "customer.Age >= 18" },
        new Rule { Description = "Name", Expression = "!string.IsNullOrEmpty(customer.Name)" }
    }
};

workflow.Validate();
workflow.Compile(parameters);

var results = workflow.Execute(parameters);

// Assert all rules passed
results.ShouldAllPass();

// Assert specific rule results
results.ShouldContainRule("Age").ShouldPass();
results.ShouldContainRule("Name").ShouldPass();
```

### Testing Rule Order

```csharp
var workflow = new Workflow
{
    Rules = new List<Rule>
    {
        new Rule { Description = "First", Priority = 10, Expression = "true" },
        new Rule { Description = "Second", Priority = 5, Expression = "true" },
        new Rule { Description = "Third", Priority = 0, Expression = "true" }
    }
};

workflow.Compile(parameters);

var results = workflow.Execute(parameters).ToList();

// Verify execution order
results[0].RuleDescription.Should().Be("First");
results[1].RuleDescription.Should().Be("Second");
results[2].RuleDescription.Should().Be("Third");
```

### Testing with Dependencies

```csharp
var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };

var workflow = new Workflow { Rules = new List<Rule> { ruleB, ruleA } };
workflow.Validate();
workflow.Compile(parameters);

var results = workflow.Execute(parameters).ToList();

// Verify dependency executed first
results[0].RuleDescription.Should().Be("A");
results[1].RuleDescription.Should().Be("B");
```

## Integration with FluentAssertions

The testing framework works seamlessly with FluentAssertions for more expressive tests.

```csharp
using FluentAssertions;
using RoslynRules.Testing;

[Fact]
public void AdultRule_WithAdultCustomer_ShouldPass()
{
    var rule = new Rule
    {
        Description = "Adult check",
        Expression = "customer.Age >= 18",
        IsActive = true
    };

    var parameters = new[]
    {
        new RuleParameter("customer", typeof(Customer), new Customer { Age = 25 })
    };

    var compiler = new ExpressionCompiler();
    rule.Compile(compiler, parameters);
    var result = rule.Execute(parameters);

    // Framework assertions
    result.ShouldPass();
    
    // FluentAssertions for complex checks
    result.Success.Should().BeTrue();
    result.RuleDescription.Should().Be("Adult check");
    result.ChildResults.Should().BeEmpty();
}
```

### Combining Both Styles

```csharp
[Fact]
public void ComplexRule_ShouldBehaveCorrectly()
{
    var result = rule.Execute(parameters);
    
    // Built-in assertions for common checks
    result.ShouldPass();
    result.ShouldHaveAllChildrenPass();
    result.ShouldNotHaveThrown();
    
    // FluentAssertions for custom checks
    result.Value.Should().BeOfType<ValidationResult>();
    var typedValue = (ValidationResult)result.Value;
    typedValue.Errors.Should().BeEmpty();
    typedValue.Warnings.Should().HaveCount(2);
}
```

## Best Practices

### 1. Test Both Success and Failure

```csharp
public class AgeRuleTests
{
    private readonly Rule _rule;
    private readonly RuleParameter[] _parameters;

    public AgeRuleTests()
    {
        _rule = new Rule
        {
            Description = "Age check",
            Expression = "customer.Age >= 18",
            IsActive = true
        };
        _parameters = new[] { new RuleParameter("customer", typeof(Customer), default) };
        var compiler = new ExpressionCompiler();
        _rule.Compile(compiler, _parameters);
    }

    [Fact]
    public void WithAdult_ShouldPass() =>
        _rule.Execute(new[] { new RuleParameter("customer", typeof(Customer), new Customer { Age = 25 }) })
            .ShouldPass();

    [Fact]
    public void WithMinor_ShouldFail() =>
        _rule.Execute(new[] { new RuleParameter("customer", typeof(Customer), new Customer { Age = 16 }) })
            .ShouldFail();

    [Fact]
    public void WithBoundary_ShouldPass() =>
        _rule.Execute(new[] { new RuleParameter("customer", typeof(Customer), new Customer { Age = 18 }) })
            .ShouldPass();
}
```

### 2. Use Test Suites for Regression Testing

```csharp
[Fact]
public void AllValidationRules_ShouldPassWithValidCustomer()
{
    var suite = new RuleTestSuite();
    
    foreach (var rule in GetAllValidationRules())
    {
        suite.AddTest(RuleTest.For(rule)
            .WithInput("customer", ValidCustomer)
            .ExpectSuccess());
    }
    
    suite.Run().ThrowOnFailure();
}
```

### 3. Test Rule Compilation Separately

```csharp
[Fact]
public void Rule_ShouldCompileSuccessfully()
{
    var rule = new Rule { Expression = "customer.Age >= 18" };
    var compiler = new ExpressionCompiler();

    var act = () => rule.Compile(compiler, parameters);
    act.Should().NotThrow();
}

[Fact]
public void Rule_WithSyntaxError_ShouldThrow()
{
    var rule = new Rule { Expression = "customer.Age >= " }; // Syntax error
    var compiler = new ExpressionCompiler();

    var act = () => rule.Compile(compiler, parameters);
    act.Should().Throw<InvalidOperationException>();
}
```

## See Also

- [API Reference: RuleResult](../api-reference.md#ruleresult)
- [Rule Action Chaining](rule-action-chaining.md) — Includes parent-child patterns
- [Examples Index](index.md)
