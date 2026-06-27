---
layout: default
title: AssemblyReferenceProvider
parent: API Reference
nav_order: 17
---

[← Back to API Reference](../api-reference.md)

# AssemblyReferenceProvider

Controls which assemblies are available to compiled expressions. Used for sandboxing.

```csharp
public class AssemblyReferenceProvider
```

---

## Default Behavior

The default provider includes a safe whitelist of common assemblies (`DefaultWhitelist`) and
excludes a set of known dangerous assemblies (`KnownDangerousAssemblies`). Matching is a
case-insensitive substring (`Contains`) match against each loaded assembly's name.

**Included by default (`DefaultWhitelist`):**
- `System.Runtime`
- `System.Private.CoreLib`
- `mscorlib`
- `netstandard`
- `System.Core`
- `System.Linq`
- `System.Linq.Expressions`
- `System.Collections`
- `System.Text.Json`
- `System.Text.RegularExpressions`
- `System.ComponentModel.Annotations`
- `RoslynRules`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.CodeAnalysis`
- `Microsoft.CodeAnalysis.CSharp`
- `Microsoft.CSharp`
- `Microsoft.CodeAnalysis.CSharp.Scripting`

**Excluded (`KnownDangerousAssemblies`, always blocked):**
- `System.IO` — file system access
- `System.IO.FileSystem` — file system access
- `System.Diagnostics.Process` — process execution
- `System.Net.Http` — network access
- `System.Net.Sockets` — network access
- `System.Net.Security` — network access
- `System.Security.Cryptography` — cryptography
- `System.Reflection.Emit` — runtime code generation
- `System.Runtime.Loader` — assembly loading
- `System.Data.SqlClient` — database access
- `System.Data.OleDb` — database access
- `System.Data.Odbc` — database access
- `Microsoft.Win32.Registry` — registry access

---

## Custom Provider

```csharp
var provider = new AssemblyReferenceProvider();
provider.AllowAssembly("MyCompany.Models");

var del = compiler.Compile<Func<Customer, bool>>(
    "customer.Name.Length > 0",
    new[] { "customer" },
    referenceProvider: provider
);
```

---

## Security Note

Even with a whitelist, never compile expressions from untrusted sources without validation. See [Security](../security.md) for full details.

---

## Related

- [ExpressionCompiler](expressioncompiler.md) — Uses provider
- [Security](../security.md) — Security guide
