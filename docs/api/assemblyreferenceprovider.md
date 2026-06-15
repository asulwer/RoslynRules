---
layout: default
title: AssemblyReferenceProvider
parent: API Reference
nav_order: 17
---

[← Back to API Reference](api-reference.md)

# AssemblyReferenceProvider

Controls which assemblies are available to compiled expressions. Used for sandboxing.

```csharp
public class AssemblyReferenceProvider
```

---

## Default Behavior

The default provider includes a safe whitelist of common assemblies and excludes dangerous ones.

**Included by default:**
- `System`
- `System.Core`
- `System.Linq`
- `System.Collections`
- `System.Text`
- `System.Text.RegularExpressions`
- `Microsoft.CSharp`

**Excluded (dangerous):**
- `System.IO` — file system access
- `System.Net` — network access
- `System.Reflection` — reflection abuse
- `System.Diagnostics.Process` — process execution
- `System.Security` — security manipulation

---

## Custom Provider

```csharp
var provider = new AssemblyReferenceProvider();
provider.AddAssembly(typeof(MyCustomType).Assembly);

var del = compiler.Compile<Func<Customer, bool>>(
    "customer.Name.Length > 0",
    new[] { "customer" },
    referenceProvider: provider
);
```

---

## Security Note

Even with a whitelist, never compile expressions from untrusted sources without validation. See [SECURITY.md](../SECURITY.md) for full details.

---

## Related

- [ExpressionCompiler](expressioncompiler.md) — Uses provider
- [SECURITY.md](../SECURITY.md) — Security guide
