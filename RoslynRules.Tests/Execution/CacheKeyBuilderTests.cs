using FluentAssertions;
using RoslynRules.Execution;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for CacheKeyBuilder reference type handling and structural vs identity hashing.
    /// </summary>
    public class CacheKeyBuilderTests
    {
        [Fact]
        public void Build_SameValueTypeParameters_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var params1 = new[] { new RuleParameter("age", typeof(int), 30) };
            var params2 = new[] { new RuleParameter("age", typeof(int), 30) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_DifferentValueTypeParameters_DifferentKey()
        {
            var ruleId = Guid.NewGuid();
            var params1 = new[] { new RuleParameter("age", typeof(int), 30) };
            var params2 = new[] { new RuleParameter("age", typeof(int), 25) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().NotBe(key2);
        }

        [Fact]
        public void Build_SameStringParameters_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var params1 = new[] { new RuleParameter("name", typeof(string), "Alice") };
            var params2 = new[] { new RuleParameter("name", typeof(string), "Alice") };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_DifferentStringParameters_DifferentKey()
        {
            var ruleId = Guid.NewGuid();
            var params1 = new[] { new RuleParameter("name", typeof(string), "Alice") };
            var params2 = new[] { new RuleParameter("name", typeof(string), "Bob") };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().NotBe(key2);
        }

        [Fact]
        public void Build_SameCollectionContents_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var list1 = new List<string> { "A", "B" };
            var list2 = new List<string> { "A", "B" };

            var params1 = new[] { new RuleParameter("items", typeof(List<string>), list1) };
            var params2 = new[] { new RuleParameter("items", typeof(List<string>), list2) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_DifferentCollectionContents_DifferentKey()
        {
            var ruleId = Guid.NewGuid();
            var list1 = new List<string> { "A", "B" };
            var list2 = new List<string> { "C", "D" };

            var params1 = new[] { new RuleParameter("items", typeof(List<string>), list1) };
            var params2 = new[] { new RuleParameter("items", typeof(List<string>), list2) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().NotBe(key2);
        }

        [Fact]
        public void Build_MutableReferenceType_DifferentInstances_DifferentKey()
        {
            var ruleId = Guid.NewGuid();
            var obj1 = new MutableCustomer { Name = "Alice", Age = 30 };
            var obj2 = new MutableCustomer { Name = "Alice", Age = 30 };

            var params1 = new[] { new RuleParameter("customer", typeof(MutableCustomer), obj1) };
            var params2 = new[] { new RuleParameter("customer", typeof(MutableCustomer), obj2) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            // Different instances should have different cache keys (identity-based)
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void Build_MutableReferenceType_SameInstance_AfterMutation_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var obj = new MutableCustomer { Name = "Alice", Age = 30 };

            var params1 = new[] { new RuleParameter("customer", typeof(MutableCustomer), obj) };
            var key1 = CacheKeyBuilder.Build(ruleId, params1);

            // Mutate the object
            obj.Name = "Bob";
            obj.Age = 40;

            var params2 = new[] { new RuleParameter("customer", typeof(MutableCustomer), obj) };
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            // Same instance should have same cache key even after mutation (identity-based)
            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_Collection_AfterMutation_DifferentKey()
        {
            var ruleId = Guid.NewGuid();
            var list = new List<string> { "A", "B" };

            var params1 = new[] { new RuleParameter("items", typeof(List<string>), list) };
            var key1 = CacheKeyBuilder.Build(ruleId, params1);

            // Mutate the collection
            list.Add("C");

            var params2 = new[] { new RuleParameter("items", typeof(List<string>), list) };
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            // Structural hashing: mutated collection should have different key
            key1.Should().NotBe(key2);
        }

        [Fact]
        public void Build_NestedCollections_SameContents_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var nested1 = new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { 3, 4 }
            };
            var nested2 = new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { 3, 4 }
            };

            var params1 = new[] { new RuleParameter("matrix", typeof(List<List<int>>), nested1) };
            var params2 = new[] { new RuleParameter("matrix", typeof(List<List<int>>), nested2) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_DeeplyNestedCollection_HitsMaxDepth()
        {
            var ruleId = Guid.NewGuid();
            // Create a deeply nested structure that exceeds MaxCollectionDepth (10)
            object deep = "leaf";
            for (int i = 0; i < 15; i++)
            {
                deep = new List<object> { deep };
            }

            var parameters = new[] { new RuleParameter("deep", typeof(object), deep) };

            // Should not throw — max depth marker should be used
            var key = CacheKeyBuilder.Build(ruleId, parameters);
            key.Should().Contain("[maxdepth]");
        }

        [Fact]
        public void Build_DifferentRuleIds_DifferentKey()
        {
            var ruleId1 = Guid.NewGuid();
            var ruleId2 = Guid.NewGuid();
            var parameters = new[] { new RuleParameter("age", typeof(int), 30) };

            var key1 = CacheKeyBuilder.Build(ruleId1, parameters);
            var key2 = CacheKeyBuilder.Build(ruleId2, parameters);

            key1.Should().NotBe(key2);
        }

        [Fact]
        public void Build_NullValue_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var params1 = new[] { new RuleParameter("name", typeof(string), null) };
            var params2 = new[] { new RuleParameter("name", typeof(string), null) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_Array_SameContents_SameKey()
        {
            var ruleId = Guid.NewGuid();
            var arr1 = new[] { 1, 2, 3 };
            var arr2 = new[] { 1, 2, 3 };

            var params1 = new[] { new RuleParameter("data", typeof(int[]), arr1) };
            var params2 = new[] { new RuleParameter("data", typeof(int[]), arr2) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().Be(key2);
        }

        [Fact]
        public void Build_Array_DifferentContents_DifferentKey()
        {
            var ruleId = Guid.NewGuid();
            var arr1 = new[] { 1, 2, 3 };
            var arr2 = new[] { 1, 2, 4 };

            var params1 = new[] { new RuleParameter("data", typeof(int[]), arr1) };
            var params2 = new[] { new RuleParameter("data", typeof(int[]), arr2) };

            var key1 = CacheKeyBuilder.Build(ruleId, params1);
            var key2 = CacheKeyBuilder.Build(ruleId, params2);

            key1.Should().NotBe(key2);
        }

        /// <summary>
        /// Mutable reference type for testing identity-based cache keys.
        /// </summary>
        private class MutableCustomer
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
        }
    }
}
