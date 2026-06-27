---
layout: default
title: AOT Compatibility
parent: Documentation
nav_order: 7
---

# AOT Compatibility

RoslynRules supports AOT deployment via **pre-compiled snapshots**. Rules cannot be compiled at runtime in AOT — they must be compiled beforehand in a JIT environment.

---

## RoslynRules Role: JIT vs AOT

| Capability | JIT Mode | AOT Mode |
|-----------|----------|----------|
| Load rules from JSON/EF | ✅ Yes | ✅ Yes |
| `Workflow.Validate()` | ✅ Yes | ✅ Yes |
| `Workflow.Compile()` (from string) | ✅ Yes | ❌ **Not supported** |
| `Workflow.Execute()` without snapshot | ✅ Yes | ❌ Throws error |
| `Workflow.Execute()` with snapshot | ✅ Yes | ✅ Yes |
| Create snapshots | ✅ Yes | ❌ **Not supported** |
| Load snapshots | ✅ Yes | ✅ Yes |

**Key principle:**
- **JIT** is the compilation environment
- **AOT** is the execution environment

---

## Why AOT Cannot Compile

AOT removes the JIT compiler entirely. RoslynRules' `Compile()` method requires:
1. `CSharpCompilation.Emit()` → generates IL
2. `AssemblyLoadContext.LoadFromStream()` → loads dynamic assembly
3. `MethodInfo.CreateDelegate()` → reflection on dynamic method

All three require a JIT compiler. AOT has none.

---

## Deployment Architecture

### Recommended: Two-Stage Deployment

```
[Rule Authoring]          [Production Runtime]
     │                            │
     ▼                            ▼
┌──────────┐              ┌──────────────┐
│ JSON/EF  │──Compile()──→│  Snapshot    │
│ Rules    │   (JIT)      │  Files       │
└──────────┘              └──────────────┘
     │                            │
     │                            │ LoadSnapshot()
     │                            ▼
     │                       ┌──────────┐
     │                       │ AOT App  │
     │                       │ Execute()│
     │                       └──────────┘
```

**JIT Admin/Authoring Tool:**
- Loads rules from JSON/EF
- Compiles with `ExpressionCompiler`
- Saves snapshots to disk/storage
- Can modify and recompile rules

**AOT Production App:**
- Loads rules from JSON/EF (for metadata)
- Loads pre-compiled snapshots
- Executes without compilation

### Alternative: Self-Contained Single-File (Not AOT)

If you need dynamic compilation in production, do not use AOT. Use:

```xml
<!-- .csproj -->
<PublishAot>false</PublishAot>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
```

This gives you:
- Single .exe file
- No runtime install needed
- Full dynamic compilation support
- Larger binary (~15-30MB vs ~5MB for AOT)

---

## Snapshot API

Snapshots are now implemented. See the [Snapshots](snapshots.html) documentation for full details.

Quick reference:

```csharp
// JIT: Compile and save snapshot
var compiled = CompiledWorkflow.Compile(workflow, parameters);
var snapshot = compiled.ToSnapshot();
File.WriteAllText("workflow.snap.json", new JsonSnapshotSerializer().Serialize(snapshot));

// AOT: Load and execute
var snapshot = SnapshotManager.LoadSnapshot(new JsonSnapshotSerializer(), "workflow.snap.json");
var workflow = SnapshotManager.RestoreWorkflow(snapshot);
```

---

## Runtime Detection

RoslynRules detects AOT at runtime and throws clear errors:

```csharp
if (!RuntimeFeature.IsDynamicCodeSupported)
{
    throw new PlatformNotSupportedException(
        "JIT compilation is not available in AOT mode. " +
        "Use pre-compiled snapshots or run in JIT mode.");
}
```

Your app can also detect mode:

```csharp
bool isAot = !RuntimeFeature.IsDynamicCodeSupported;

var serializer = new JsonSnapshotSerializer();

if (isAot)
{
    // Load a pre-compiled snapshot and restore the workflow.
    var snapshot = SnapshotManager.LoadSnapshot(serializer, "snapshots/workflow.snap.json");
    var workflow = SnapshotManager.RestoreWorkflow(snapshot);
}
else
{
    // Compile and save a snapshot for later AOT execution.
    var compiled = CompiledWorkflow.Compile(workflow, parameters);
    var snapshot = compiled.ToSnapshot();
    SnapshotManager.SaveSnapshot(snapshot, serializer, "snapshots/workflow.snap.json");
}
```

---

## AOT-Safe APIs (No Compilation Needed)

These work in AOT without snapshots:

- `Workflow` / `Rule` model creation
- `Workflow.Validate()` — syntax validation
- `RuleResult` creation and inspection
- `RuleContext` result storage
- `GraphAlgorithms.TopologicalSort()`

These **require** snapshots in AOT:

- `Workflow.Execute()`
- `Workflow.Compile()`
- `ExpressionCompiler`

---

## CI Integration

The `.github/workflows/aot.yml` workflow validates AOT-safe APIs compile without linker errors:

```bash
dotnet test RoslynRules.Tests --filter "FullyQualifiedName~AotCompatibilityTests"
```

These tests cover model creation, validation, `RuleResult`, `RuleContext`, and execution order — all without runtime compilation.

---

## Known Limitations

- `System.Linq.Expressions` is not AOT-friendly; RoslynRules avoids it
- `Assembly.LoadFrom` and `Reflection.Emit` are unsupported in native AOT
- Dynamic generic instantiation (`MakeGenericType`) may be trimmed

All JIT-dependent APIs are annotated with `[RequiresUnreferencedCode]` to surface these limitations at build time.
