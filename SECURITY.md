# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| latest  | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability in RoslynRules, please report it by opening a private security advisory on GitHub or emailing the maintainers directly.

Please do not open public issues for security vulnerabilities.

---

## Expression Injection Warning

**RoslynRules compiles user-supplied C# expression strings into executable code at runtime.** This is a powerful feature, but it carries inherent security risks similar to SQL injection or eval-based attacks.

### Built-in Sandboxing (v1.0.3+)

Starting with v1.0.3, RoslynRules ships with a **default assembly whitelist** that restricts what compiled expressions can access. The compiler only references assemblies matching the whitelist and explicitly excludes known dangerous assemblies.

**Default whitelisted assemblies:**
- `System.Runtime`, `System.Private.CoreLib` — core .NET types
- `System.Linq`, `System.Linq.Expressions` — LINQ and expression trees
- `System.Collections` — collections
- `System.Text.Json` — JSON serialization
- `System.Text.RegularExpressions` — regex
- `RoslynRules` — the rule engine itself
- `Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CSharp` — Roslyn compiler internals

**Blocked (dangerous) assemblies:**
- `System.IO` / `System.IO.FileSystem` — file system access
- `System.Diagnostics.Process` — process spawning
- `System.Net.Http` / `System.Net.Sockets` — network access
- `System.Security.Cryptography` — cryptographic operations
- `System.Reflection.Emit` — dynamic code generation
- `System.Runtime.Loader` — assembly loading
- `System.Data.SqlClient` / `System.Data.OleDb` / `System.Data.Odbc` — database access
- `Microsoft.Win32.Registry` — registry access

**Customizing the whitelist:**

You can supply your own `AssemblyReferenceProvider` when creating an `ExpressionCompiler`:

```csharp
var whitelist = new[] { "MyApp.Domain", "MyApp.Services" };
var blocked = new[] { "System.IO" }; // extra blocks beyond defaults
var provider = new AssemblyReferenceProvider(whitelist, blocked);

var compiler = new ExpressionCompiler(referenceProvider: provider);
var rule = new Rule { Expression = "MyApp.Domain.IsValid(data)" };
rule.Compile(compiler, parameters);
```

> ⚠️ **Important:** Sandboxing prevents accidental access but is not a complete security boundary. Determined attackers with deep .NET knowledge may still find escape routes. Combine sandboxing with the other mitigations below.

### What expressions can access (without sandboxing override)

Because expressions are compiled as C# code, they can invoke any method available from whitelisted assemblies:
- The `System` namespace (e.g., `DateTime.Now`, `Guid.NewGuid()`)
- LINQ (`System.Linq`)
- Collections (`System.Collections.Generic`)
- JSON (`System.Text.Json`)
- Any custom assemblies you explicitly whitelist

### Example of a malicious expression

```csharp
// DON'T DO THIS with untrusted input
var rule = new Rule
{
    Expression = @"System.IO.File.Delete(@""C:\\important.txt""); true"
};
```

This expression would compile and execute, deleting a file.

### Mitigation strategies

1. **Use the default sandboxing.** Do not override the `AssemblyReferenceProvider` unless you understand the security implications.

2. **Never compile expressions from untrusted sources** (user input, external APIs, uploaded files) without validation.

3. **Validate expressions before compilation.** Use `rule.Validate()` to check syntax, but note that syntax validation does NOT prevent malicious code — it only checks for valid C#.

4. **Use a whitelist approach.** Parse the expression's syntax tree and reject expressions containing forbidden method calls or types.

5. **Run in a sandboxed environment.** Consider executing rules in a separate process or container with limited permissions.

6. **Prefer static rule definitions.** Define rules in code or configuration files rather than accepting dynamic expressions at runtime.

### What we do NOT protect against

- **Denial of Service:** An expression like `while(true) {}` or heavy CPU computation can hang or degrade performance.
- **Resource exhaustion:** Expressions can allocate large amounts of memory.
- **Side effects:** Expressions can mutate objects passed as parameters if those objects are mutable.

See issue #24 for the sandboxing implementation and related discussions.
