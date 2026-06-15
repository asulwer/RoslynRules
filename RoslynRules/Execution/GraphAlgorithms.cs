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

            var inDegree = new Dictionary<Guid, int>();
            var adjacency = new Dictionary<Guid, List<Guid>>();

            foreach (var node in nodeList)
            {
                var id = getId(node);
                inDegree[id] = 0;
                adjacency[id] = new List<Guid>();
            }

            foreach (var node in nodeList)
            {
                var depId = getDependencyId(node);
                if (depId.HasValue && adjacency.ContainsKey(depId.Value))
                {
                    adjacency[depId.Value].Add(getId(node));
                    inDegree[getId(node)]++;
                }
            }

            var result = new List<T>();
            var queue = new SortedSet<T>(priorityComparer);

            // Start with all nodes that have no dependencies
            foreach (var node in nodeList.Where(n => inDegree[getId(n)] == 0))
            {
                queue.Add(node);
            }

            while (queue.Count > 0)
            {
                var current = queue.Min;
                if (current == null) break;
                queue.Remove(current);
                result.Add(current);

                var currentId = getId(current);
                foreach (var neighborId in adjacency[currentId])
                {
                    inDegree[neighborId]--;
                    if (inDegree[neighborId] == 0)
                    {
                        var neighbor = nodeList.First(n => getId(n) == neighborId);
                        queue.Add(neighbor);
                    }
                }
            }

            // Cycle detection: if we didn't process all nodes, there's a cycle
            if (result.Count != nodeList.Count)
            {
                var unprocessed = nodeList.First(n => !result.Any(r => getId(r) == getId(n)));
                throw new CircularReferenceException(
                    getId(unprocessed),
                    $"Dependency cycle detected at node '{unprocessed}'");
            }

            return result;
        }
    }
}
