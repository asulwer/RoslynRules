---
layout: default
title: Troubleshooting
nav_order: 8
---

# Troubleshooting

Common issues and solutions.

---

## Compilation Errors

### "The type or namespace name 'X' could not be found"

**Cause:** Your expression references a type that isn't in the default assembly whitelist.

**Fix:** Add the assembly to `AssemblyReferenceProvider`:
```csharp
var provider = new AssemblyReferenceProvider(new[] {
    "System",
    "System.Core",
    "MyApp.Models"  // Your assembly
});
workflow.Compile(parameters, referenceProvider: provider);
```

---

### "Cannot implicitly convert type 'X' to 'bool'"

**Cause:** The expression doesn't return a boolean. Rules require `Expression` to evaluate to `bool`.

**Fix:** Wrap in a comparison or boolean expression:
```csharp
// Wrong
Expression = "customer.Age"

// Right
Expression = "customer.Age >= 18"
```

---

### "await can only be used in async methods"

**Cause:** The expression contains `await` but RoslynRules didn't detect it as async.

**Fix:** Ensure `await` is a real await expression, not inside a string literal or variable name:
```csharp
// Correct — auto-detected as async
Expression = "await GetPriceAsync(productId) > 100"

// Incorrect — "awaiting" is a variable name, not a keyword
Expression = "awaiting > 0"  // This is sync
```

---

## Execution Errors

### "Rule has not been compiled"

**Cause:** `Execute()` called before `Compile()`.

**Fix:** Compile once before executing:
```csharp
rule.Compile(compiler, parameters);
var result = rule.Execute(parameters);  // Now works
```

---

### "Object reference not set to an instance of an object" inside expression

**Cause:** Your expression dereferences a null value.

**Fix:** Add null checks or use null-conditional operators:
```csharp
// Fails if customer is null
Expression = "customer.Age >= 18"

// Safe
Expression = "customer?.Age >= 18 ?? false"
```

---

### Rule returns `Success = false` but should pass

**Cause:** The expression evaluates to `false`. Check your logic.

**Debug:**
```csharp
var result = rule.Execute(parameters);
Console.WriteLine(result.Value);  // The actual boolean result
```

---

## Performance Issues

### Compilation is slow on first run

**Expected:** First compile takes ~50ms. This is Roslyn compiling to IL.

**Mitigation:**
- Pre-compile at startup, not on first request
- Use `ExpressionCompiler` singleton (caches across calls)
- Consider `RuleTemplate` for repeated patterns

---

### Memory grows over time

**Cause:** AssemblyLoadContexts accumulate if `maxCompilesBeforeRecycle` is too high.

**Fix:** Lower the recycle threshold:
```csharp
var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 100);
```

Or force unload:
```csharp
compiler.Unload();
GC.Collect();
GC.WaitForPendingFinalizers();
```

---

### Parallel execution is slower than sequential

**Cause:** Overhead exceeds benefit for trivial expressions.

**Rule of thumb:**
- < 5 rules: sequential
- 5-20 rules: measure both
- > 20 rules or CPU-intensive expressions: parallel

---

## JSON / Serialization

### "Failed to deserialize workflow from JSON"

**Cause:** Invalid JSON or missing required fields.

**Fix:**
1. Validate JSON syntax
2. Ensure required fields: `description`, `rules` array
3. Check that `expression` strings are valid C#

---

### Deserialized rules fail to compile

**Cause:** JSON only stores strings — parameter types aren't included.

**Fix:** You must re-compile with parameter types after loading:
```csharp
var workflow = JsonRuleLoader.LoadWorkflowFromFile("rules.json");
workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
```

---

## Dependency Issues

### "Circular reference detected"

**Cause:** Rule A depends on Rule B, and Rule B depends on Rule A (directly or transitively).

**Fix:** Restructure to break the cycle. Use `ChildRules` for structural nesting instead:
```csharp
// Circular (bad)
ruleA.DependsOnRuleId = ruleB.Id;
ruleB.DependsOnRuleId = ruleA.Id;

// Restructure (good)
ruleA.ChildRules.Add(ruleB);  // B evaluated as part of A
```

---

### "Depends on rule X which does not exist"

**Cause:** `DependsOnRuleId` points to a rule ID not in the workflow, or the target rule is inactive.

**Fix:** Ensure the target rule:
1. Is in the same `Workflow` or `RuleBatch`
2. Has `IsActive = true`
3. Is compiled before the dependent rule executes

---

## GitHub Pages / Docs

### Docs site not updating after push

**Cause:** GitHub Pages builds can take 5-10 minutes.

**Fix:**
1. Check the repo Settings → Pages → Build status
2. Ensure `_config.yml` is valid YAML
3. Verify Jekyll theme is supported (`remote_theme: just-the-docs`)

---

## Still Stuck?

- Check [GitHub Issues](https://github.com/asulwer/RoslynRules/issues)
- Review [API Reference](api-reference.md) for method details
- See [Examples](examples/index.md) for working patterns
