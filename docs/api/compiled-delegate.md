---
layout: default
title: CompiledDelegate
parent: API Reference
nav_order: 9
---

[← Back to API Reference](api-reference.md)

# CompiledDelegate

Fast invocation wrappers for compiled delegates. Eliminates the overhead of `DynamicInvoke` by extracting parameter values and calling the delegate directly. Supports sync, async, single-parameter, and multi-parameter signatures.

```csharp
internal abstract class CompiledDelegate
internal sealed class CompiledFunc<TParam, TReturn> : CompiledDelegate
internal sealed class CompiledAction<TParam> : CompiledDelegate
internal sealed class CompiledAsyncFunc<TParam, TReturn> : CompiledDelegate
internal sealed class CompiledAsyncAction<TParam> : CompiledDelegate
internal sealed class CompiledMultiParamDelegate : CompiledDelegate
internal sealed class CompiledAsyncMultiParamDelegate : CompiledDelegate
internal static class CompiledDelegateFactory
```

**Namespace:** `RoslynRules.Models`

**Key characteristics:**
- **Abstract base** (`CompiledDelegate`) with a single `Invoke(object?)` method
- **Typed wrappers** for single-parameter delegates avoid `DynamicInvoke` overhead
- **Async wrappers** support both sync-blocking (`Invoke`) and true async (`InvokeAsync`) paths
- **Multi-parameter delegates** fall back to `DynamicInvoke` (slower but necessary)
- **`CompiledDelegateFactory`** uses reflection to auto-detect the correct wrapper type
- Marked with `[RequiresUnreferencedCode]` — may not work with trimming or AOT

---

## CompiledDelegate (Base)

### Methods

| Method | Return | Description |
|--------|--------|-------------|
| `Invoke(object? parameter)` | `object?` | Invokes the delegate with the extracted parameter value |

---

## Typed Single-Parameter Wrappers

### `CompiledFunc<TParam, TReturn>`

Wraps `Func<TParam, TReturn>`.

```csharp
var del = new CompiledFunc<string, bool>(s => s.Length > 0);
var result = del.Invoke("hello"); // returns true (as object?)
```

### `CompiledAction<TParam>`

Wraps `Action<TParam>` (void return).

```csharp
var del = new CompiledAction<string>(s => Console.WriteLine(s));
var result = del.Invoke("hello"); // returns null
```

### `CompiledAsyncFunc<TParam, TReturn>`

Wraps `Func<TParam, Task<TReturn>>`.

```csharp
var del = new CompiledAsyncFunc<string, int>(async s =>
{
    await Task.Delay(1);
    return s.Length;
});

// Sync-blocking (uses ConfigureAwait(false) to avoid deadlock)
var result = del.Invoke("hello");

// True async
var resultAsync = await del.InvokeAsync("hello");
```

### `CompiledAsyncAction<TParam>`

Wraps `Func<TParam, Task>` (async void).

```csharp
var del = new CompiledAsyncAction<string>(async s =>
{
    await File.WriteAllTextAsync("log.txt", s);
});

del.Invoke("log entry");           // blocks
await del.InvokeAsync("log entry");  // async
```

---

## Multi-Parameter Wrappers

### `CompiledMultiParamDelegate`

Wraps any delegate with **more than one parameter**. Falls back to `DynamicInvoke`.

```csharp
Func<int, int, int> add = (a, b) => a + b;
var del = new CompiledMultiParamDelegate(add);

// Parameter is expected to be RuleParameter[]
var result = del.Invoke(new[]
{
    new RuleParameter("a", typeof(int), 5),
    new RuleParameter("b", typeof(int), 3)
}); // returns 8 (as object?)
```

### `CompiledAsyncMultiParamDelegate`

Wraps async multi-parameter delegates.

```csharp
Func<int, int, Task<int>> addAsync = async (a, b) => { await Task.Yield(); return a + b; };
var del = new CompiledAsyncMultiParamDelegate(addAsync);

var result = del.Invoke(new[] { new RuleParameter("a", typeof(int), 5), new RuleParameter("b", typeof(int), 3) });
var resultAsync = await del.InvokeAsync(new[] { ... });
```

---

## CompiledDelegateFactory

### Methods

#### `Wrap(Delegate)`

Creates the appropriate `CompiledDelegate` wrapper from a raw `Delegate`.

```csharp
public static CompiledDelegate Wrap(Delegate del)
```

**Detection logic:**

1. If the delegate has **more than 1 parameter** → `CompiledMultiParamDelegate` or `CompiledAsyncMultiParamDelegate`
2. If the return type is `Task` → `CompiledAsyncAction<TParam>`
3. If the return type is `Task<T>` → `CompiledAsyncFunc<TParam, T>`
4. If the return type is `void` → `CompiledAction<TParam>`
5. Otherwise → `CompiledFunc<TParam, TReturn>`

**Parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| `del` | `Delegate` | The raw compiled delegate to wrap |

**Returns**

`CompiledDelegate` — The typed wrapper for fast invocation.

**Attributes**

`[RequiresUnreferencedCode("RoslynRules uses reflection to inspect delegate signatures and instantiate generic wrappers. This code may not work correctly with trimming or AOT.")]`

---

## Usage Example

### Basic wrapping with the factory

```csharp
using RoslynRules.Models;

// Simple sync function
Func<string, bool> isValid = s => !string.IsNullOrEmpty(s);
var compiled = CompiledDelegateFactory.Wrap(isValid);

// Fast invoke (no DynamicInvoke)
var result = compiled.Invoke("hello"); // true

// Async function
Func<int, Task<int>> doubleAsync = async x => { await Task.Delay(1); return x * 2; };
var compiledAsync = CompiledDelegateFactory.Wrap(doubleAsync);

// Sync-blocking path
var syncResult = compiledAsync.Invoke(5); // 10

// True async path
var asyncResult = await ((CompiledAsyncFunc<int, int>)compiledAsync).InvokeAsync(5); // 10
```

### Multi-parameter fallback

```csharp
Func<string, int, bool> checkLength = (s, max) => s.Length <= max;
var compiled = CompiledDelegateFactory.Wrap(checkLength);

// Must use RuleParameter[] for multi-parameter delegates
var result = compiled.Invoke(new[]
{
    new RuleParameter("s", typeof(string), "hello"),
    new RuleParameter("max", typeof(int), 10)
}); // true
```

### Integration with Rule execution

```csharp
public class RuleExecutor
{
    private readonly CompiledDelegate _compiledExpression;

    public RuleExecutor(Rule rule, ExpressionCompiler compiler)
    {
        var rawDelegate = compiler.Compile(rule.Expression, rule.Parameters);
        _compiledExpression = CompiledDelegateFactory.Wrap(rawDelegate);
    }

    public bool Evaluate(object parameter)
    {
        var result = _compiledExpression.Invoke(parameter);
        return (bool)result!;
    }
}
```

---

## Performance Notes

| Delegate Type | Wrapper | Invoke Overhead |
|--------------|---------|-----------------|
| `Func<T, R>` | `CompiledFunc<T, R>` | Direct cast + call (fast) |
| `Action<T>` | `CompiledAction<T>` | Direct cast + call (fast) |
| `Func<T, Task<R>>` | `CompiledAsyncFunc<T, R>` | Direct cast + block/await |
| `Func<T1, T2, R>` | `CompiledMultiParamDelegate` | `DynamicInvoke` (slower) |
| `Func<T1, T2, Task<R>>` | `CompiledAsyncMultiParamDelegate` | `DynamicInvoke` + block/await |

For best performance, prefer **single-parameter** expressions in rules. Multi-parameter support exists for flexibility but incurs reflection overhead.

---

## Related

- [ExpressionCompiler](expressioncompiler.md) — Compiles C# expressions to delegates
- [Delegate Types](delegate-types.md) — Supported expression signatures
- [Rule](rule.md) — Uses `CompiledDelegate` internally for expression evaluation
- [AOT Compatibility](aot-compatibility.md) — Notes on trimming and AOT limitations
