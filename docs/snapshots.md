---
layout: default
title: Snapshots & AOT
nav_order: 5
---

# Snapshots & AOT Compatibility

RoslynRules supports two deployment modes: **JIT (Just-In-Time)** and **AOT (Ahead-Of-Time)**. This guide explains the two-stage workflow and how to use snapshots for AOT-compatible deployments.

## Overview

| Mode | Compilation | Best For |
|------|-------------|----------|
| **JIT** | Rules compiled at runtime using Roslyn | Development, dynamic rule updates, server-side with full .NET runtime |
| **AOT** | Rules pre-compiled, snapshots loaded at runtime | Native AOT deployments, trimmed/self-contained apps, iOS, WASM |

## The Core Problem

Roslyn compilation requires dynamic code generation, which is **not available** in:
- Native AOT published apps (`PublishAot=true`)
- Aggressively trimmed applications
- iOS (platform restrictions)
- Some WASM configurations

**The Solution:** Pre-compile rules in a JIT environment, save them as snapshots, then load those snapshots in AOT environments.

## Two-Stage Architecture

```
┌─────────────────┐                    ┌─────────────────┐
│   JIT Stage     │   ─────────────►   │   AOT Stage     │
│                 │   Save Snapshots   │                 │
│  Compile rules  │                    │  Load snapshots │
│  with Roslyn    │                    │  Execute only    │
│                 │                    │  (no compile)    │
└─────────────────┘                    └─────────────────┘
   Development                            Production
   environment                           environment
```

## Stage 1: JIT — Create Snapshots

In your development environment (or a build server with full .NET):

```csharp
using RoslynRules;
using RoslynRules.Models;
using RoslynRules.Snapshots;
using RoslynRules.Json; // or RoslynRules.Xml

// 1. Define your workflow
var workflow = new Workflow
{
    Description = "Order Validation",
    Rules =
    {
        new Rule
        {
            Description = "Check order total",
            Expression = "order.Total > 0",
            Action = "result.IsValid = true",
            Priority = 10
        },
        new Rule
        {
            Description = "Check customer status",
            Expression = "customer.IsActive",
            DependsOnRuleId = previousRule.Id
        }
    }
};

// 2. Compile the workflow (JIT only)
var parameters = new[]
{
    new RuleParameter("order", typeof(Order), null),
    new RuleParameter("customer", typeof(Customer), null)
};

workflow.Compile(parameters);

// 3. Create a snapshot
var snapshot = SnapshotManager.CreateSnapshot(
    CompiledWorkflow.Compile(workflow, parameters));

// 4. Save to file (AOT-safe format)
var serializer = new JsonSnapshotSerializer();
SnapshotManager.SaveSnapshot(snapshot, serializer, "orders.snapshot.json");
```

### Key Points for JIT Stage

- ✅ Can use `Workflow.Compile()` — full Roslyn compilation
- ✅ Can create snapshots with `SnapshotManager.CreateSnapshot()`
- ✅ Can save to JSON, XML, or custom formats
- ❌ Requires full .NET runtime (no AOT)

## Stage 2: AOT — Load and Execute

In your production AOT environment:

```csharp
using RoslynRules.Snapshots;
using RoslynRules.Json;

// 1. Load the snapshot (no compilation needed!)
var serializer = new JsonSnapshotSerializer();
var snapshot = SnapshotManager.LoadSnapshot(serializer, "orders.snapshot.json");

// 2. Restore the workflow
var workflow = SnapshotManager.RestoreWorkflow(snapshot);

// 3. Execute (AOT-safe!)
var parameters = new[]
{
    new RuleParameter("order", typeof(Order), order),
    new RuleParameter("customer", typeof(Customer), customer)
};

var results = workflow.Execute(parameters);
```

### Key Points for AOT Stage

- ✅ Can load snapshots — no reflection or compilation needed
- ✅ Can execute pre-compiled rules
- ✅ Works with `PublishAot=true`
- ❌ Cannot call `Compile()` — will throw `AotCompatibilityException`

## Runtime Detection

Check if you're running in AOT mode:

```csharp
if (AotCompatibility.IsAot)
{
    Console.WriteLine("Running in AOT mode — use snapshots");
}
else
{
    Console.WriteLine("Running in JIT mode — can compile rules");
}
```

## Common Patterns

### Pattern 1: Build-Time Snapshot Generation

```csharp
// In a console app that runs during CI/CD
public static void Main(string[] args)
{
    AotCompatibility.ThrowIfAot(nameof(Main)); // Must run in JIT

    foreach (var ruleFile in Directory.GetFiles("rules", "*.json"))
    {
        var workflow = JsonRuleLoader.LoadWorkflowFromFile(ruleFile);
        var parameters = GetParametersFor(ruleFile);

        workflow.Compile(parameters);
        var snapshot = SnapshotManager.CreateSnapshot(
            CompiledWorkflow.Compile(workflow, parameters));

        var outputPath = Path.Combine("snapshots", Path.GetFileName(ruleFile) + ".snapshot");
        SnapshotManager.SaveSnapshot(snapshot, new JsonSnapshotSerializer(), outputPath);
    }
}
```

### Pattern 2: Snapshot Cache

```csharp
public class SnapshotCache
{
    private readonly Dictionary<string, WorkflowSnapshot> _cache = new();

    public WorkflowSnapshot GetOrLoad(string name, ISnapshotSerializer serializer)
    {
        if (!_cache.TryGetValue(name, out var snapshot))
        {
            snapshot = SnapshotManager.LoadSnapshot(serializer, $"snapshots/{name}.json");
            _cache[name] = snapshot;
        }
        return snapshot;
    }
}
```

### Pattern 3: Fallback to JIT in Development

```csharp
public class RuleEngineFactory
{
    public IRuleEngine Create(string workflowName)
    {
        if (AotCompatibility.IsAot)
        {
            // Production: Load from snapshot
            var snapshot = _snapshotCache.Get(workflowName);
            return SnapshotManager.RestoreWorkflow(snapshot);
        }
        else
        {
            // Development: Load from JSON, compile
            var workflow = JsonRuleLoader.LoadWorkflowFromFile($"rules/{workflowName}.json");
            workflow.Compile(_parameters);
            return workflow;
        }
    }
}
```

## Troubleshooting

### "JIT compilation is not available in AOT mode"

**Error:** `AotCompatibilityException: Compile is not supported in AOT mode`

**Solution:** You're calling `Compile()` in an AOT environment. Use pre-generated snapshots instead.

### "Failed to deserialize workflow from JSON"

**Error:** JSON parsing error when loading snapshots

**Solution:** Ensure you're using the same serializer version for save and load. Consider versioning your snapshot files:

```csharp
// Save with version
var snapshot = new WorkflowSnapshot
{
    Version = new RuleVersion(1, 0, 0),
    // ...
};

// Check version on load
if (!snapshot.Version.IsCompatibleWith(expectedVersion))
{
    throw new InvalidOperationException("Snapshot version mismatch");
}
```

### "Snapshot doesn't include compiled delegates"

**Behavior:** Rules execute slowly or throw compilation errors

**Solution:** Snapshots contain rule definitions, not compiled IL. You must still call `Compile()` in JIT environments before creating snapshots. In AOT, the rules were already compiled before snapshotting.

## Migration from JIT to AOT

1. **Identify** rules that need to be pre-compiled
2. **Create** a snapshot generation tool (console app)
3. **Run** the tool in your CI/CD pipeline
4. **Package** snapshots with your AOT app
5. **Update** your app to load snapshots instead of compiling

## See Also

- [AOT Compatibility](./aot-compatibility.md) — Detailed AOT detection and troubleshooting
- [Performance Tuning](./performance-tuning.md) — Optimize rule execution
- [API Reference: SnapshotManager](../api/snapshotmanager.md)
- [API Reference: ISnapshotSerializer](../api/isnapshotserializer.md)
