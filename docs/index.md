---
layout: default
title: Documentation
nav_order: 1
has_children: false
---

# RoslynRules Documentation

High-performance .NET rules engine with Roslyn compilation, typed delegates, and async support.

### [Migration Guide](migration.md) — Moving from Microsoft.RulesEngine

## What Makes It Fast

<table>
  <thead>
    <tr><th>Feature</th><th>How It Works</th></tr>
  </thead>
  <tbody>
    <tr><td><strong>Roslyn Compilation</strong></td><td>Expressions compile to IL once, execute as native code</td></tr>
    <tr><td><strong>Typed Delegates</strong></td><td>Direct <code>Func&lt;T,R&gt;</code> calls — no <code>DynamicInvoke</code> overhead</td></tr>
    <tr><td><strong>Single Parameter</strong></td><td>One input, one output — no array allocation per call</td></tr>
    <tr><td><strong>Immutable Rules</strong></td><td>Lock after compile — zero thread contention</td></tr>
    <tr><td><strong>Parallel Execution</strong></td><td><code>Parallel.For</code> for independent rule evaluation</td></tr>
  </tbody>
</table>

## Extension Packages

### RoslynRules.EntityFrameworkCore

Install: `dotnet add package RoslynRules.EntityFrameworkCore`

Provides EF Core entity models (`RuleEntity`, `WorkflowEntity`) with lazy loading support. Convert to sealed domain models for execution via `ToDomainModel()`. See the [EF Core example](examples/ef-serialization.md).

### RoslynRules.Json

Install: `dotnet add package RoslynRules.Json`

JSON serialization for workflows and rules. See [JSON Serialization](api/json-serialization.md).

## EF Core Integration Note

<code>Rule</code> is <code>sealed</code> to enforce immutability after compilation. This prevents EF Core lazy loading proxies from working (proxies require subclassing). Use <strong>eager loading</strong> (<code>Include</code>/<code>ThenInclude</code>) or <strong>explicit loading</strong> (<code>Load()</code>) instead. See the <a href="examples/ef-serialization.md">EF Core example</a>.

## When to Use Which Execution Mode

<table>
  <thead>
    <tr><th>Mode</th><th>Best For</th></tr>
  </thead>
  <tbody>
    <tr><td><code>Execute()</code></td><td>Few simple rules, low latency requirements</td></tr>
    <tr><td><code>ExecuteParallel()</code></td><td>Many CPU-intensive rules</td></tr>
    <tr><td><code>ExecuteParallelAsync()</code></td><td>Rules with async I/O (DB, HTTP)</td></tr>
    <tr><td><code>ExecuteAsync()</code></td><td>Streaming results, async iterators</td></tr>
  </tbody>
</table>

## Example

```csharp
var rule = new Rule
{
    Expression = "customer.Age >= 18",
    Action = "customer.IsAdult = true"
};

var wf = new Workflow { Rules = new List<Rule> { rule } };
var compileParam = RuleParameter.ForCompile("customer", typeof(Customer));
var executeParam = RuleParameter.ForExecute("customer", typeof(Customer), customer);

wf.Validate();
wf.Compile(new[] { compileParam });
var results = wf.Execute(new[] { executeParam });
```

## Dependency Injection

Register `IRuleEngine` in your DI container:

```csharp
using RoslynRules.Abstractions;

services.AddSingleton<IRuleEngine, Workflow>();
```

[Get Started →](getting-started.md)

## API Reference

<a href="api-reference.html">API Reference →</a> — Complete API documentation organized by component:

| Section | Contents |
|---------|----------|
| <a href="api/rule.html">Rule</a> | Properties, compilation, execution, lifecycle |
| <a href="api/rule-localization.html">Localization</a> | i18n, `DescriptionKey`, `IRuleDescriptionProvider` |
| <a href="api/rule-visualization.html">Visualization</a> | DOT/Mermaid dependency graphs |
| <a href="api/rule-metrics.html">Metrics</a> | Eval count, avg time, failure rate |
| <a href="api/workflow.html">Workflow</a> | Execution modes, parallel, async |
| <a href="api/rulecontext.html">RuleContext</a> | Dependency result access |
| <a href="api/iruleengine.html">IRuleEngine</a> | DI registration, mocking |
| <a href="api/rulebatch.html">RuleBatch</a> | Batch evaluation |
| <a href="api/expressioncompiler.html">ExpressionCompiler</a> | Compile API, ALC recycling |
| <a href="api/delegate-types.html">Delegate Types</a> | Supported signatures |
| <a href="api/json-serialization.html">JSON</a> | Serialize/deserialize |
| <a href="api/rule-templates.html">Templates</a> | Placeholder substitution |
| <a href="api/rule-predicates.html">Predicates</a> | 25+ built-in validation factories |
| <a href="api/rule-priority.html">Priority</a> | Execution order |
| <a href="api/lifecycle-events.html">Lifecycle Events</a> | OnRuleExecuting/OnRuleExecuted |
| <a href="api/result-caching.html">Caching</a> | Memoization, TTL |
| <a href="api/exceptions.html">Exceptions</a> | Typed exception hierarchy |
| <a href="api/assemblyreferenceprovider.html">Sandboxing</a> | Assembly whitelist/blacklist |
| <a href="aot-compatibility.html">AOT</a> | Native AOT compatibility |

## Examples

| Topic | Description |
|-------|-------------|
| <a href="examples/index.html">Examples Index</a> | All examples |
| <a href="examples/rule-action-chaining.html">Action Chaining</a> | `DependsOnRuleId` patterns |
| <a href="examples/ef-serialization.html">EF Core</a> | Persistence with Entity Framework |
| <a href="examples/testing-framework.html">Testing</a> | `RuleTest` and assertions |
| <a href="examples/streaming-and-cancellation.html">Streaming</a> | `IAsyncEnumerable` results |
| <a href="examples/real-world-use-cases.html">Use Cases</a> | Production patterns |
| <a href="examples/when-to-use-what.html">When to Use What</a> | Execution mode comparison |
