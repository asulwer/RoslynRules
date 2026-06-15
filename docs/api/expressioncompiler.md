---
layout: default
title: ExpressionCompiler
parent: API Reference
nav_order: 6
---

[← Back to API Reference](api-reference.md)

# ExpressionCompiler

Compiles C# expression strings into strongly-typed delegates. Results are cached for reuse.

```csharp
public class ExpressionCompiler
```

---

## Constructor

```csharp
public ExpressionCompiler(int maxCompilesBeforeRecycle = 1000)
```

| Parameter | Description |
|-----------|-------------|
| `maxCompilesBeforeRecycle` | Maximum unique compilations before the internal `AssemblyLoadContext` is unloaded and recreated. Set to `0` for no limit. |

---

## Methods

### `Compile<TDelegate>(string expression, string[] parameterNames, string[]? additionalNamespaces, AssemblyReferenceProvider? referenceProvider)`

Compiles a C# expression into a typed delegate.

```csharp
var compiler = new ExpressionCompiler();
var del = compiler.Compile<Func<int, bool>>(
    "x > 0",
    new[] { "x" }
);
bool result = del(42);  // true
```

**Parameters:**
- `expression` — C# expression body
- `parameterNames` — Ordered names matching delegate signature
- `additionalNamespaces` — Extra `using` namespaces (e.g., `"Demo.Models"`)
- `referenceProvider` — Custom assembly whitelist for sandboxing (optional)

### `Unload()`

Forces immediate unload of the current `AssemblyLoadContext` and clears the delegate cache.

```csharp
compiler.Unload();  // Reclaim memory
```

### `CompileCount`

Returns the number of unique compilations performed.

```csharp
Console.WriteLine($"Compilations: {compiler.CompileCount}");
```

### `CurrentContextName`

Returns the current `AssemblyLoadContext` name for diagnostics.

```csharp
Console.WriteLine($"Context: {compiler.CurrentContextName}");
```

---

## ALC Recycling

Each compilation loads a new assembly into a **collectible** `AssemblyLoadContext`. To prevent memory growth:

| Strategy | When |
|----------|------|
| Automatic recycling | Set `maxCompilesBeforeRecycle` (default: 1000) |
| Manual unload | Call `Unload()` when memory pressure detected |
| Reuse delegates | The compiler caches delegates by expression signature |

---

## Sandboxing

Use `AssemblyReferenceProvider` to restrict which assemblies are available to compiled expressions.

```csharp
var whitelist = new AssemblyReferenceProvider();
// Only allow specific assemblies
var del = compiler.Compile<Func<Customer, bool>>(
    "customer.Name.Length > 0",
    new[] { "customer" },
    referenceProvider: whitelist
);
```

---

## AOT/Trimming

Reflection-heavy methods are annotated with `[RequiresUnreferencedCode]`. For trimmed apps:

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="RoslynRules" />
</ItemGroup>
```

---

## Related

- [Delegate Types](delegate-types.md) — Supported signatures
- [AssemblyReferenceProvider](assemblyreferenceprovider.md) — Sandboxing
