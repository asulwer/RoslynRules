---
layout: default
title: GraphAlgorithms
parent: API Reference
nav_order: 7
---

[← Back to API Reference](api-reference.md)

# GraphAlgorithms

General-purpose graph algorithms for dependency resolution. Used internally by `Workflow` to order rules before execution.

```csharp
public static class GraphAlgorithms
```

**Namespace:** `RoslynRules.Execution`

**Key characteristics:**
- **Static utility class** — no state, thread-safe
- Uses **Kahn's algorithm** for topological sorting
- Supports **priority-based tie-breaking** within the same dependency level
- Detects **circular references** and throws `CircularReferenceException`

---

## Methods

### `TopologicalSort<T>`

Topologically sorts nodes so that dependencies always appear before their dependents.

```csharp
public static List<T> TopologicalSort<T>(
    IEnumerable<T> nodes,
    Func<T, Guid> getId,
    Func<T, Guid?> getDependencyId,
    IComparer<T> priorityComparer)
```

**Parameters**

| Parameter | Type | Description |
|-----------|------|-------------|
| `nodes` | `IEnumerable<T>` | All nodes to sort |
| `getId` | `Func<T, Guid>` | Extracts the unique identifier for each node |
| `getDependencyId` | `Func<T, Guid?>` | Extracts the dependency ID, or `null` if none |
| `priorityComparer` | `IComparer<T>` | Comparer for `SortedSet`: `Compare(a, b) < 0` means `a` has **higher** priority than `b` |

**Returns**

`List<T>` — Nodes in execution order (dependencies before dependents).

**Exceptions**

| Exception | Trigger |
|-----------|---------|
| `CircularReferenceException` | A dependency cycle is detected |

**Behavior**

1. **Fast path:** If no nodes have dependencies, returns the list sorted by priority descending.
2. **Dependency graph:** Builds an in-degree map and adjacency list from the nodes.
3. **Kahn's algorithm:** Processes nodes with zero in-degree, decrementing in-degrees of dependents.
4. **Priority ordering:** Within each level, `priorityComparer` determines which node processes first.
5. **Cycle detection:** If the result count doesn't match the input count, a cycle exists.

---

## Usage Example

```csharp
using RoslynRules.Execution;
using RoslynRules.Exceptions;

public record WorkflowNode(
    Guid Id,
    int Priority,
    string Name,
    Guid? DependsOnId = null);

public class DependencyResolver
{
    public List<WorkflowNode> ResolveOrder(List<WorkflowNode> nodes)
    {
        // Higher Priority value = execute first within the same level
        var comparer = Comparer<WorkflowNode>.Create((a, b) =>
        {
            var cmp = b.Priority.CompareTo(a.Priority); // descending
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Name, b.Name);
        });

        try
        {
            return GraphAlgorithms.TopologicalSort(
                nodes,
                n => n.Id,
                n => n.DependsOnId,
                comparer);
        }
        catch (CircularReferenceException ex)
        {
            Console.WriteLine($"Cycle detected at node {ex.RuleId}");
            throw;
        }
    }
}

// Example nodes
var idA = Guid.NewGuid();
var idB = Guid.NewGuid();
var idC = Guid.NewGuid();

var nodes = new List<WorkflowNode>
{
    new WorkflowNode(idC, 0, "Final Check", idB),   // depends on B
    new WorkflowNode(idB, 5, "Mid Check",  idA),   // depends on A
    new WorkflowNode(idA, 10, "First Check")       // no deps
};

var resolver = new DependencyResolver();
var ordered = resolver.ResolveOrder(nodes);

// Result: A → B → C (dependencies first, then priority)
foreach (var node in ordered)
    Console.WriteLine(node.Name);
```

---

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Empty list | Returns empty list |
| Single node | Returns single-element list |
| No dependencies | Sorts purely by priority |
| Missing dependency | Node treated as having no dependencies (silently ignored) |
| Equal priority | Order determined by comparer (stable if comparer is stable) |
| Circular reference | Throws `CircularReferenceException` with offending node ID |

---

## Related

- [Workflow](workflow.md) — Container that uses `GraphAlgorithms` for rule ordering
- [Rule](rule.md) — Individual rule with `DependsOnRuleId`
- [Exceptions](exceptions.md) — `CircularReferenceException` details
