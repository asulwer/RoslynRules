---
layout: default
title: Security
parent: Documentation
nav_order: 7
---

[<- Back to Documentation Index](index.md)

# Security Guide

RoslynRules compiles and executes arbitrary C# expressions. This is powerful but requires careful attention to security.

---

## Threat Model

| Threat | Mitigation |
|--------|-----------|
| **Expression injection** | Validate all user input; use `ValidateSemantics` |
| **Malicious assembly references** | Whitelist assemblies with `AssemblyReferenceProvider` |
| **Infinite loops** | Set per-rule `Timeout` |
| **Memory exhaustion** | Set `maxCompilesBeforeRecycle`; monitor ALC count |
| **Type access** | Restrict namespaces; avoid `System.Reflection` in expressions |

---

## AssemblyReferenceProvider Hardening

By default, RoslynRules whitelists common assemblies:

```csharp
var defaultProvider = new AssemblyReferenceProvider(
    AssemblyReferenceProvider.DefaultWhitelist);
```

### Custom Whitelist

Restrict to only the assemblies your expressions need:

```csharp
var hardenedProvider = new AssemblyReferenceProvider(new[]
{
    "System",
    "System.Core",
    "System.Linq",
    "System.Linq.Expressions",
    "System.Collections",
    "MyApp.Models"  // Your types only
});

workflow.Compile(parameters, referenceProvider: hardenedProvider);
```

### Block Dangerous APIs

Never include these in your whitelist:

```csharp
// DANGEROUS — do NOT include
"System.IO",           // File system access
"System.Net",            // Network access
"System.Diagnostics",    // Process spawning
"System.Reflection",     // Type introspection
"System.Runtime.InteropServices",  // Native interop
```

---

## Sandboxing Limits

The `ExpressionCompiler` uses a collectible `AssemblyLoadContext` per compilation. Old contexts are unloaded automatically.

### Tune ALC Recycling

```csharp
// Recycle after 1000 compilations (default)
var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 1000);

// Force unload if needed
compiler.Unload();
```

### Monitor Compile Count

```csharp
if (compiler.CompileCount > 500)
{
    _logger.LogWarning("Compiler approaching recycle threshold");
}
```

---

## Input Validation

Always validate user-provided expressions before compilation:

```csharp
// Syntax validation (fast, no compiler needed)
rule.Validate();

// Semantic validation (compiles, catches undefined variables)
Rule.ValidateSemantics(userExpression, typeof(Customer), "customer");
```

### Expression Injection Prevention

```csharp
// BAD — direct user input into expression
var expression = $"customer.Name == '{userInput}'";  // SQL injection-style attack

// GOOD — parameterized logic
var expression = "customer.Name == expectedName";
var parameters = new[]
{
    new RuleParameter("customer", typeof(Customer), customer),
    new RuleParameter("expectedName", typeof(string), userInput)
};
```

---

## Timeout Protection

Set per-rule timeouts to prevent infinite loops:

```csharp
var rule = new Rule
{
    Expression = "ComputeInfiniteLoop(customer)",
    Timeout = TimeSpan.FromSeconds(5)
};
```

---

## Dependency Patterns

Use `DependsOnRuleId` to chain rules securely — data flows through `RuleContext`, not shared state:

```csharp
var auth = new Rule { Id = authId, Expression = "user.IsAuthenticated" };
var admin = new Rule
{
    Expression = "context.GetValue<bool>(authId)",
    DependsOnRuleId = authId
};
```

---

## Security Checklist

- [ ] Whitelist only required assemblies
- [ ] Validate all user expressions with `ValidateSemantics`
- [ ] Set `Timeout` on untrusted rules
- [ ] Monitor `CompileCount` and ALC recycling
- [ ] Never include `System.IO`, `System.Net`, `System.Diagnostics`
- [ ] Use `RuleContext` for dependency data, not shared mutable state
- [ ] Log compilation failures for audit trail

---

## Related

- [AssemblyReferenceProvider API](api/assemblyreferenceprovider.md)
- [Performance Tuning](performance-tuning.md)
- [Rule](api/rule.md) — `Timeout`, `ValidateSemantics`
