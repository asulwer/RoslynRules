---
layout: default
title: Architecture
nav_order: 2
---

# RoslynRules Architecture

How the engine turns C# expressions into compiled delegates.

---

## Overview

```
Expression string
      ↓
[C# Syntax Tree] → Syntax validation
      ↓
[C# Compilation] → Roslyn.Emit → IL assembly
      ↓
[AssemblyLoadContext] → Load into collectible ALC
      ↓
[Delegate] → Typed Func<T1,...,T16,TReturn>
      ↓
[Execution] → Direct invocation (no reflection, no interpreters)
```

---

## The Compilation Pipeline

### 1. Expression Parsing

RoslynRules parses your expression into a proper C# syntax tree using `Microsoft.CodeAnalysis.CSharp`:

```csharp
var tree = CSharpSyntaxTree.ParseText("customer.Age >= 18");
```

This catches syntax errors immediately — missing semicolons, unmatched parentheses, invalid operators — before any compilation cost is incurred.

### 2. Semantic Validation (Optional)

`Rule.ValidateSemantics()` runs a lightweight semantic analysis using Roslyn's `CSharpCompilation` without emitting IL:

```csharp
// Validates that "customer" resolves to a known type
Rule.ValidateSemantics("customer.Age >= 18", typeof(Customer), "customer");
```

This catches:
- Undefined variables
- Missing type members
- Type mismatch errors

### 3. Full Compilation

When you call `Rule.Compile()`, RoslynRules:

1. **Builds a delegate signature** from your parameter types:
   - 1-16 parameters → `Func<T1,...,T16,TReturn>` or `Action<T1,...,T16>`
   - Async expressions → `Func<Task<TReturn>>` or `Func<Task>`

2. **Generates a complete C# source file** wrapping your expression:
   ```csharp
   public class CompiledRule {
       public bool Evaluate(Customer customer) {
           return customer.Age >= 18;
       }
   }
   ```

3. **Compiles to IL** using `CSharpCompilation` with:
   - Whitelist-controlled assembly references (`AssemblyReferenceProvider`)
   - Optimized release build
   - In-memory assembly (no disk output)

4. **Loads into collectible ALC**:
   ```csharp
   var alc = new ExpressionAssemblyLoadContext();
   var assembly = alc.LoadFromStream(ilStream);
   ```

5. **Extracts the delegate** via reflection (once, at compile time only):
   ```csharp
   var method = assembly.GetType("CompiledRule").GetMethod("Evaluate");
   var del = method.CreateDelegate(typeof(Func<Customer, bool>));
   ```

### 4. Caching

Compiled delegates are cached in a `ConcurrentDictionary<CacheKey, Delegate>`:

- **Key**: Expression string + parameter types + additional namespaces
- **Value**: Compiled delegate
- **Lifetime**: Application lifetime (or until `compiler.Unload()`)

Subsequent calls with the same signature return the cached delegate instantly.

---

## Execution Model

### Sequential

```csharp
foreach (var rule in workflow.Rules)
{
    var result = rule.CompiledDelegate.Invoke(parameters);
    context.StoreResult(rule.Id, result);
}
```

### Parallel

```csharp
Parallel.ForEach(rules, rule => {
    var result = rule.CompiledDelegate.Invoke(parameters);
    // Thread-safe result storage
});
```

### Async

```csharp
var asyncDel = (Func<Customer, Task<bool>>)rule.CompiledDelegate;
var result = await asyncDel(customer);
```

---

## AssemblyLoadContext Lifecycle

Collectible ALCs prevent memory leaks in long-running apps:

```
Compilation #1    → ALC #1
Compilation #2      → ALC #1 (same)
...
Compilation #1000 → ALC #1
Compilation #1001 → ALC #2 (new, ALC #1 marked for GC)
```

Tuning:
```csharp
var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 1000);
```

- Lower = more frequent unloading, higher GC pressure
- Higher = less overhead, more memory stable
- `0` = never auto-recycle (manual `Unload()` only)

---

## Dependency Resolution

`DependsOnRuleId` creates a DAG (directed acyclic graph):

```
Rule A (validate) ─┐
                   ├──→ Rule C (process)
Rule B (check) ────┘
```

1. **Topological sort** (Kahn's algorithm) determines execution order
2. Dependencies execute before dependent rules
3. Results are stored in `RuleContext` for downstream access
4. Cycles are detected at `Validate()` time

---

## Result Caching (Memoization)

Per-rule opt-in caching:

```csharp
var rule = new Rule {
    CacheDuration = TimeSpan.FromMinutes(5)
};
```

- Cache key = rule ID + all parameter values (deep hash)
- Thread-safe via `ConcurrentDictionary`
- Lazy expiration on read (no background timer)
- `ClearCache()` forces eviction

---

## Thread Safety

| Component | Strategy |
|-----------|----------|
| `ExpressionCompiler` | ConcurrentDictionary cache, lock-free reads |
| `Workflow.Execute()` | Rule-level parallel, result collection synchronized |
| `Rule.Execute()` | Immutable after `Compile()`, no locks needed |
| `RuleCache` | ConcurrentDictionary + Interlocked metrics |
| `RuleContext` | ConcurrentDictionary for result storage |

---

## Performance Characteristics

| Phase | Time | Notes |
|-------|------|-------|
| First compile | ~50ms | Roslyn compilation + ALC creation |
| Cached compile | ~1μs | Dictionary lookup |
| Execution | ~10ns | Direct delegate call |
| Parallel execution | ~10ns × rules / cores | Near-linear scaling |

---

## Related

- [Performance Tuning](performance-tuning.md) — Configure for your workload
- [Security](security.md) — Assembly sandboxing and threat model
- [API Reference](api-reference.md) — Class-level documentation
