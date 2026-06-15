---
layout: default
title: API Reference
nav_order: 3
has_children: true
---

# API Reference

Complete reference for all RoslynRules public APIs, organized by component.

## Quick Navigation

### Core Models

<table>
  <thead>
    <tr><th>Class</th><th>Purpose</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api/rule.html">Rule</a></td><td>Individual rule with Expression, Action, child rules</td></tr>
    <tr><td><a href="api/workflow.html">Workflow</a></td><td>Container for top-level rules</td></tr>
    <tr><td><a href="api/ruleresult.html">RuleResult</a></td><td>Execution result with child traceability</td></tr>
    <tr><td><a href="api/ruleparameter.html">RuleParameter</a></td><td>Parameter definition (name, type, value)</td></tr>
    <tr><td><a href="api/rule-diagnostics.html">RuleDiagnostics</a></td><td>Diagnostics, logging, and auditing model</td></tr>
    <tr><td><a href="api/rule-lifecycle-events.html">RuleLifecycleEvents</a></td><td>OnRuleExecuting/OnRuleExecuted event args</td></tr>
    <tr><td><a href="api/compiled-delegate.html">CompiledDelegate</a></td><td>Fast invocation wrapper for compiled delegates</td></tr>
  </tbody>
</table>

### Execution & Context

<table>
  <thead>
    <tr><th>Class</th><th>Purpose</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api/rulecontext.html">RuleContext</a></td><td>Access dependency rule results during execution</td></tr>
    <tr><td><a href="api/iruleengine.html">IRuleEngine</a></td><td>Abstraction for DI and mocking</td></tr>
    <tr><td><a href="api/rulebatch.html">RuleBatch</a></td><td>Batch evaluation for 10+ rules</td></tr>
    <tr><td><a href="api/graph-algorithms.html">GraphAlgorithms</a></td><td>Topological sort for dependency resolution</td></tr>
  </tbody>
</table>

### Compilation

<table>
  <thead>
    <tr><th>Class</th><th>Purpose</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api/expressioncompiler.html">ExpressionCompiler</a></td><td>Compile C# expressions to typed delegates</td></tr>
    <tr><td><a href="api/delegate-types.html">Delegate Types</a></td><td>Supported expression signatures</td></tr>
  </tbody>
</table>

### Configuration & Data

<table>
  <thead>
    <tr><th>Class</th><th>Purpose</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api/json-serialization.html">JSON Serialization</a></td><td>Save/load rules from JSON</td></tr>
    <tr><td><a href="api/rule-templates.html">Rule Templates</a></td><td>Reusable parameterized rule templates</td></tr>
    <tr><td><a href="api/rule-predicates.html">Rule Predicates</a></td><td>Built-in validation factory methods</td></tr>
  </tbody>
</table>

### Runtime Features

<table>
  <thead>
    <tr><th>Topic</th><th>Purpose</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api/rule-priority.html">Rule Priority</a></td><td>Control execution order</td></tr>
    <tr><td><a href="api/lifecycle-events.html">Lifecycle Events</a></td><td>Pre/post execution hooks</td></tr>
    <tr><td><a href="api/result-caching.html">Result Caching</a></td><td>Memoization for expensive rules</td></tr>
  </tbody>
</table>

### Exceptions & Diagnostics

<table>
  <thead>
    <tr><th>Class</th><th>Purpose</th></tr>
  </thead>
  <tbody>
    <tr><td><a href="api/exceptions.html">Exceptions</a></td><td>Typed exception hierarchy</td></tr>
    <tr><td><a href="api/exceptions.html#validationerror">ValidationError</a></td><td>Structured validation errors</td></tr>
  </tbody>
</table>

---

**Looking for examples?** See the <a href="examples/">Examples</a> section for real-world use cases.
