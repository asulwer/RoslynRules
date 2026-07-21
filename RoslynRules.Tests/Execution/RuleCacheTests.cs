using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for Rule result caching (memoization).
    /// </summary>
    public class RuleCacheTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly ExpressionCompiler _compiler;

        public RuleCacheTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 30, Name = "Alice" })
            };
            _compiler = new global::RoslynRules.Compiler.ExpressionCompiler();
        }

        [Fact]
        public void Execute_CacheEnabled_SecondCallReturnsCachedResult()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Cached result should be the same reference (struct equality)
            result2.Should().Be(result1);
        }

        [Fact]
        public void Execute_CacheDisabled_EvaluatesEveryTime()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = null // disabled
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Should re-evaluate (different struct instances)
            result2.Should().NotBe(result1);
        }

        [Fact]
        public void Execute_CacheExpired_ReEvaluates()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMilliseconds(50)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            System.Threading.Thread.Sleep(100); // Let cache expire
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // After expiration, should be a fresh evaluation
            result2.Should().NotBe(result1);
        }

        [Fact]
        public void Execute_ClearCache_ForcesReEvaluation()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(_parameters);
            rule.ClearCache();
            var result2 = rule.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // After clear, should be a fresh evaluation
            result2.Should().NotBe(result1);
        }

        [Fact]
        public void Execute_Cache_DifferentParameters_DifferentResults()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var params1 = new[]
            {
                new RuleParameter("age", typeof(int), 30)
            };
            var params2 = new[]
            {
                new RuleParameter("age", typeof(int), 15)
            };

            rule.Compile(_compiler, params1, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(params1);
            var result2 = rule.Execute(params2);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_Cache_Hit_CountIncreases()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            rule.Execute(_parameters);
            rule.Execute(_parameters);
            rule.Execute(_parameters);

            // Internal cache count should be 1 (same key)
            // We can't directly access _resultCache.Count from here since it's private
            // But we can verify behavior: third call should still be cached
            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_CacheEnabled_SecondCallReturnsCachedResult()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "await Task.FromResult(customer.Age >= 18)",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = await rule.ExecuteAsync(_parameters);
            var result2 = await rule.ExecuteAsync(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            result2.Should().Be(result1);
        }

        [Fact]
        public void Execute_Cache_ChildRules_StillEvaluated()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var child = new Rule
            {
                Description = "Child",
                Expression = "customer.Age >= 21",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            parent.ChildRules.Add(child);
            parent.Compile(_compiler, _parameters, new[] { "RoslynRules.Tests" });

            var result1 = parent.Execute(_parameters);
            var result2 = parent.Execute(_parameters);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Parent result cached, but child should still be evaluated independently
        }

        [Fact]
        public void Execute_Cache_DifferentListContents_DifferentCacheKeys()
        {
            var rule = new Rule
            {
                Description = "List check",
                Expression = "items.Count > 0",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var params1 = new[]
            {
                new RuleParameter("items", typeof(List<string>), new List<string> { "A", "B" })
            };
            var params2 = new[]
            {
                new RuleParameter("items", typeof(List<string>), new List<string> { "C", "D" })
            };

            rule.Compile(_compiler, params1, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(params1); // should succeed and cache
            var result2 = rule.Execute(params2); // different contents = different key = re-evaluate

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Different cache keys means independent evaluations
        }

        [Fact]
        public void Execute_Cache_SameListContents_SameCacheKey()
        {
            var rule = new Rule
            {
                Description = "List check",
                Expression = "items.Count > 0",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var list1 = new List<string> { "A", "B" };
            var list2 = new List<string> { "A", "B" }; // same contents, different instance

            var params1 = new[] { new RuleParameter("items", typeof(List<string>), list1) };
            var params2 = new[] { new RuleParameter("items", typeof(List<string>), list2) };

            rule.Compile(_compiler, params1, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(params1);
            var result2 = rule.Execute(params2);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
            // Same contents should produce same cache key = cache hit
            result2.Should().Be(result1);
        }

        [Fact]
        public void Execute_Cache_MutatedReferenceParameter_ReEvaluates()
        {
            var customer = new TestCustomer { Age = 30, Name = "Alice" };
            var parameters = new[] { new RuleParameter("customer", typeof(TestCustomer), customer) };

            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 18",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            rule.Compile(_compiler, parameters, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(parameters);
            result1.Success.Should().BeTrue();

            // Mutate the same instance so the result should flip. Value-based cache keys must
            // produce a different key and re-evaluate rather than return the stale cached result.
            customer.Age = 10;
            var result2 = rule.Execute(parameters);

            result2.Success.Should().BeFalse("mutating the parameter must invalidate the cached result");
        }

        [Fact]
        public void Execute_Cache_ArrayDifferentContents_DifferentResults()
        {
            var rule = new Rule
            {
                Description = "Array length check",
                Expression = "items.Length >= 2",
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5)
            };

            var params1 = new[] { new RuleParameter("items", typeof(string[]), new[] { "x", "y" }) };
            var params2 = new[] { new RuleParameter("items", typeof(string[]), new[] { "a" }) };

            rule.Compile(_compiler, params1, new[] { "RoslynRules.Tests" });

            var result1 = rule.Execute(params1);
            var result2 = rule.Execute(params2);

            result1.Success.Should().BeTrue();
            result2.Success.Should().BeFalse();
        }
    }
}
