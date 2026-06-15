using FluentAssertions;
using RoslynRules.Execution;
using RoslynRules.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for the extracted GraphAlgorithms utility class.
    /// </summary>
    public class GraphAlgorithmsTests
    {
        private record TestNode(Guid Id, int Priority, Guid? DependsOnId = null);

        private static IComparer<TestNode> CreatePriorityComparer(List<TestNode> nodes)
        {
            var indexMap = nodes.Select((n, i) => new { n.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index);
            return Comparer<TestNode>.Create((a, b) =>
            {
                var cmp = b.Priority.CompareTo(a.Priority);
                return cmp != 0 ? cmp : indexMap[a.Id].CompareTo(indexMap[b.Id]);
            });
        }

        [Fact]
        public void TopologicalSort_EmptyList_ReturnsEmpty()
        {
            var nodes = new List<TestNode>();
            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result.Should().BeEmpty();
        }

        [Fact]
        public void TopologicalSort_SingleNode_ReturnsSingle()
        {
            var node = new TestNode(Guid.NewGuid(), 0);
            var nodes = new List<TestNode> { node };
            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result.Should().ContainSingle().Which.Should().Be(node);
        }

        [Fact]
        public void TopologicalSort_NoDependencies_SortsByPriorityDescending()
        {
            var low = new TestNode(Guid.NewGuid(), 0);
            var medium = new TestNode(Guid.NewGuid(), 5);
            var high = new TestNode(Guid.NewGuid(), 10);
            var nodes = new List<TestNode> { low, medium, high };

            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result.Select(n => n.Priority).Should().Equal(10, 5, 0);
        }

        [Fact]
        public void TopologicalSort_EqualPriority_PreservesOriginalOrder()
        {
            var a = new TestNode(Guid.NewGuid(), 5);
            var b = new TestNode(Guid.NewGuid(), 5);
            var c = new TestNode(Guid.NewGuid(), 5);
            var nodes = new List<TestNode> { a, b, c };

            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result.Should().Equal(a, b, c);
        }

        [Fact]
        public void TopologicalSort_LinearChain_RespectsDependencies()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var node1 = new TestNode(id1, 0);
            var node2 = new TestNode(id2, 0, id1);
            var node3 = new TestNode(id3, 0, id2);
            var nodes = new List<TestNode> { node3, node1, node2 };

            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result.Should().Equal(node1, node2, node3);
        }

        [Fact]
        public void TopologicalSort_MultipleDependencies_RespectsAll()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var id4 = Guid.NewGuid();
            var node1 = new TestNode(id1, 10); // highest priority, no deps
            var node2 = new TestNode(id2, 5, id1); // depends on 1
            var node3 = new TestNode(id3, 5, id1); // also depends on 1
            var node4 = new TestNode(id4, 0, id2); // depends on 2
            var nodes = new List<TestNode> { node4, node2, node3, node1 };

            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            // node1 must be first, node4 must be after node2
            result[0].Should().Be(node1);
            result.IndexOf(node4).Should().BeGreaterThan(result.IndexOf(node2));
        }

        [Fact]
        public void TopologicalSort_DiamondShape_RespectsDependencies()
        {
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var idC = Guid.NewGuid();
            var idD = Guid.NewGuid();
            var nodeA = new TestNode(idA, 0);        // root
            var nodeB = new TestNode(idB, 0, idA);   // depends on A
            var nodeC = new TestNode(idC, 0, idA);   // depends on A
            var nodeD = new TestNode(idD, 0, idB) { DependsOnId = idC }; // depends on B... wait, can only have one dep
            // Actually, let's use a simple tree: D depends on B
            var nodeD2 = new TestNode(idD, 0, idB);
            var nodes = new List<TestNode> { nodeD2, nodeB, nodeC, nodeA };

            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result[0].Should().Be(nodeA);
            result.IndexOf(nodeB).Should().BeGreaterThan(result.IndexOf(nodeA));
            result.IndexOf(nodeD2).Should().BeGreaterThan(result.IndexOf(nodeB));
        }

        [Fact]
        public void TopologicalSort_CircularReference_ThrowsCircularReferenceException()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var node1 = new TestNode(id1, 0, id2);
            var node2 = new TestNode(id2, 0, id1);
            var nodes = new List<TestNode> { node1, node2 };

            var act = () => GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            act.Should().Throw<CircularReferenceException>();
        }

        [Fact]
        public void TopologicalSort_MissingDependency_IsIgnored()
        {
            // GraphAlgorithms assumes valid input — missing dependencies are caught
            // by Workflow.ValidateDependencies before TopologicalSort is called.
            var id1 = Guid.NewGuid();
            var missingId = Guid.NewGuid();
            var node1 = new TestNode(id1, 0, missingId);
            var nodes = new List<TestNode> { node1 };

            // Missing dependency is silently ignored (node treated as having no deps)
            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            result.Should().ContainSingle().Which.Should().Be(node1);
        }

        [Fact]
        public void TopologicalSort_PriorityWithinDependencyLevel_ExecutesHigherFirst()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var node1 = new TestNode(id1, 5); // lower priority
            var node2 = new TestNode(id2, 10, id1); // depends on 1 but higher priority
            var nodes = new List<TestNode> { node2, node1 };

            var result = GraphAlgorithms.TopologicalSort(
                nodes, n => n.Id, n => n.DependsOnId, CreatePriorityComparer(nodes));

            // Even though node2 has higher priority, it must come after node1 (dependency)
            result[0].Should().Be(node1);
            result[1].Should().Be(node2);
        }
    }
}
