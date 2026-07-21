using System;
using System.Collections.Generic;
using System.Linq;
using RoslynRules.Exceptions;

namespace RoslynRules.Execution
{
    /// <summary>
    /// General-purpose graph algorithms for dependency resolution.
    /// Static utility class — no state, thread-safe.
    /// </summary>
    public static class GraphAlgorithms
    {
        /// <summary>
        /// Topologically sorts nodes using Kahn's algorithm.
        /// Nodes with no dependencies come first. Within the same level,
        /// order is determined by the provided priority comparer.
        /// </summary>
        /// <typeparam name="T">Node type.</typeparam>
        /// <param name="nodes">All nodes to sort.</param>
        /// <param name="getId">Unique identifier for each node.</param>
        /// <param name="getDependencyId">Dependency ID, or null if none.</param>
        /// <param name="priorityComparer">Comparer for SortedSet: Compare(a,b) &lt; 0 means a has higher priority than b.</param>
        /// <returns>Nodes in execution order (dependencies before dependents).</returns>
        /// <exception cref="CircularReferenceException">Thrown when a dependency cycle is detected.</exception>
        public static List<T> TopologicalSort<T>(
            IEnumerable<T> nodes,
            Func<T, Guid> getId,
            Func<T, Guid?> getDependencyId,
            IComparer<T> priorityComparer)
        {
            var nodeList = nodes.ToList();
            if (nodeList.Count == 0)
                return nodeList;

            // Fast path: no dependencies at all — sort by priority descending
            if (!nodeList.Any(n => getDependencyId(n).HasValue))
            {
                return nodeList.OrderBy(n => n, priorityComparer).ToList();
            }

            // Lookup by id so neighbor resolution is O(1) instead of O(n) per edge.
            var byId = new Dictionary<Guid, T>(nodeList.Count);
            foreach (var node in nodeList)
                byId[getId(node)] = node;

            var inDegree = new Dictionary<Guid, int>(nodeList.Count);
            var adjacency = new Dictionary<Guid, List<Guid>>(nodeList.Count);

            foreach (var node in nodeList)
            {
                var id = getId(node);
                inDegree[id] = 0;
                adjacency[id] = new List<Guid>();
            }

            foreach (var node in nodeList)
            {
                var depId = getDependencyId(node);
                // A dependency on a node outside this set (inactive/missing) is treated as
                // "no dependency": the node runs as independent. Callers validate dangling
                // dependencies separately; all execution paths tolerate them uniformly.
                if (depId.HasValue && adjacency.ContainsKey(depId.Value))
                {
                    adjacency[depId.Value].Add(getId(node));
                    inDegree[getId(node)]++;
                }
            }

            var result = new List<T>(nodeList.Count);

            // Ready set of nodes with no remaining in-edges. A List (not SortedSet) is used
            // deliberately: SortedSet collapses elements the comparer reports as equal, which
            // would silently drop distinct nodes of equal priority and misfire cycle detection.
            var ready = new List<T>();
            foreach (var node in nodeList)
                if (inDegree[getId(node)] == 0)
                    ready.Add(node);

            while (ready.Count > 0)
            {
                // Select the highest-priority ready node (Compare(a,b) < 0 => a first).
                var minIndex = 0;
                for (int i = 1; i < ready.Count; i++)
                {
                    if (priorityComparer.Compare(ready[i], ready[minIndex]) < 0)
                        minIndex = i;
                }

                var current = ready[minIndex];
                ready.RemoveAt(minIndex);
                result.Add(current);

                foreach (var neighborId in adjacency[getId(current)])
                {
                    if (--inDegree[neighborId] == 0)
                        ready.Add(byId[neighborId]);
                }
            }

            // Cycle detection: if we didn't process all nodes, there's a cycle
            if (result.Count != nodeList.Count)
            {
                var processed = new HashSet<Guid>(result.Count);
                foreach (var r in result)
                    processed.Add(getId(r));

                var unprocessed = nodeList.First(n => !processed.Contains(getId(n)));
                throw new CircularReferenceException(
                    getId(unprocessed),
                    $"Dependency cycle detected at node '{unprocessed}'");
            }

            return result;
        }
    }
}
