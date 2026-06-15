using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Benchmarks for Rule execution performance:
    /// - Simple rule execution
    /// - Rule with child rules (hierarchical)
    /// - Rule with action
    /// - Sync vs async execution
    /// - Rule with vs without caching
    /// - Deep rule hierarchies
    /// </summary>
    public static class RuleExecutionBenchmark
    {
        private static readonly ExpressionCompiler _compiler = new();
        private static readonly RuleParameter[] _parameters = new[]
        {
            new RuleParameter("customer", typeof(ExecutionBenchmarkCustomer), new ExecutionBenchmarkCustomer { Age = 25, IsActive = true })
        };

        public static void Run()
        {
            Console.WriteLine("=== Rule Execution Benchmark ===\n");

            SimpleRuleBenchmark();
            Console.WriteLine();

            RuleWithActionBenchmark();
            Console.WriteLine();

            ChildRulesBenchmark();
            Console.WriteLine();

            DeepHierarchyBenchmark();
            Console.WriteLine();

            SyncVsAsyncBenchmark();
            Console.WriteLine();

            CacheBenchmark();
            Console.WriteLine();

            InactiveRuleBenchmark();
        }

        private static void SimpleRuleBenchmark()
        {
            Console.WriteLine("--- Simple Rule Execution ---");
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "customer.Age >= 18"
            };
            rule.Compile(_compiler, _parameters);

            int iterations = 1000000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                rule.Execute(_parameters);
            }
            sw.Stop();

            Console.WriteLine($"  {iterations:N0} executions: {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Average: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call");
        }

        private static void RuleWithActionBenchmark()
        {
            Console.WriteLine("--- Rule with Action ---");
            var rule = new Rule
            {
                Description = "Age check with action",
                Expression = "customer.Age >= 18",
                Action = "customer.IsAdult = true"
            };
            rule.Compile(_compiler, _parameters);

            int iterations = 500000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var customer = new ExecutionBenchmarkCustomer { Age = 25, IsActive = true };
                var param = new RuleParameter("customer", typeof(ExecutionBenchmarkCustomer), customer);
                rule.Execute(param);
            }
            sw.Stop();

            Console.WriteLine($"  {iterations:N0} executions: {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Average: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call");
        }

        private static void ChildRulesBenchmark()
        {
            Console.WriteLine("--- Rule with Child Rules ---");

            foreach (var childCount in new[] { 1, 5, 10, 20 })
            {
                var parent = new Rule
                {
                    Description = "Parent",
                    Expression = "true"
                };

                for (int i = 0; i < childCount; i++)
                {
                    parent.ChildRules.Add(new Rule
                    {
                        Description = $"Child {i}",
                        Expression = "customer.Age > 0"
                    });
                }

                parent.Compile(_compiler, _parameters);

                int iterations = 100000 / Math.Max(childCount, 1);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    parent.Execute(_parameters);
                }
                sw.Stop();

                Console.WriteLine($"  {childCount} children: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} executions ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }
        }

        private static void DeepHierarchyBenchmark()
        {
            Console.WriteLine("--- Deep Rule Hierarchy ---");

            foreach (var depth in new[] { 1, 3, 5, 10 })
            {
                var root = new Rule { Description = "Root", Expression = "true" };
                var current = root;
                for (int i = 0; i < depth; i++)
                {
                    var child = new Rule { Description = $"Level {i}", Expression = "customer.Age > 0" };
                    current.ChildRules.Add(child);
                    current = child;
                }

                root.Compile(_compiler, _parameters);

                int iterations = 50000 / depth;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    root.Execute(_parameters);
                }
                sw.Stop();

                Console.WriteLine($"  Depth {depth}: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} executions ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }
        }

        private static void SyncVsAsyncBenchmark()
        {
            Console.WriteLine("--- Sync vs Async Execution ---");

            // Sync rule
            var syncRule = new Rule
            {
                Description = "Sync",
                Expression = "customer.Age >= 18"
            };
            syncRule.Compile(_compiler, _parameters);

            int syncIterations = 100000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < syncIterations; i++)
            {
                syncRule.Execute(_parameters);
            }
            sw.Stop();
            var syncMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"  Sync:  {syncMs:F2}ms for {syncIterations:N0} calls ({syncMs / syncIterations * 1000:F2} ns/call)");

            // Async rule
            var asyncRule = new Rule
            {
                Description = "Async",
                Expression = "await Task.FromResult(customer.Age >= 18)"
            };
            asyncRule.Compile(_compiler, _parameters, new[] { "System.Threading.Tasks" });

            int asyncIterations = 50000;
            sw.Restart();
            for (int i = 0; i < asyncIterations; i++)
            {
                var task = asyncRule.ExecuteAsync(_parameters);
                // Don't await — measuring fire-and-forget overhead
            }
            sw.Stop();
            var asyncMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"  Async: {asyncMs:F2}ms for {asyncIterations:N0} calls ({asyncMs / asyncIterations * 1000:F2} ns/call) [fire-and-forget]");

            // Actually await some
            sw.Restart();
            for (int i = 0; i < 10000; i++)
            {
                var task = asyncRule.ExecuteAsync(_parameters);
                task.GetAwaiter().GetResult();
            }
            sw.Stop();
            var awaitMs = sw.Elapsed.TotalMilliseconds;
            Console.WriteLine($"  Async: {awaitMs:F2}ms for 10,000 calls with await ({awaitMs / 10000 * 1000:F2} ns/call)");
        }

        private static void CacheBenchmark()
        {
            Console.WriteLine("--- Rule Caching ---");

            // Without cache
            {
                var rule = new Rule
                {
                    Description = "No cache",
                    Expression = "customer.Age >= 18"
                };
                rule.Compile(_compiler, _parameters);

                int iterations = 100000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    rule.Execute(_parameters);
                }
                sw.Stop();
                Console.WriteLine($"  No cache:      {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls");
            }

            // With cache
            {
                var rule = new Rule
                {
                    Description = "With cache",
                    Expression = "customer.Age >= 18",
                    CacheDuration = TimeSpan.FromSeconds(60)
                };
                rule.Compile(_compiler, _parameters);

                int iterations = 100000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    rule.Execute(_parameters);
                }
                sw.Stop();
                Console.WriteLine($"  With cache:    {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls");
            }

            // Cache hit vs miss
            {
                var rule = new Rule
                {
                    Description = "Cache test",
                    Expression = "customer.Age >= 18",
                    CacheDuration = TimeSpan.FromSeconds(60)
                };
                rule.Compile(_compiler, _parameters);

                // Warm cache
                rule.Execute(_parameters);

                int iterations = 100000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    rule.Execute(_parameters);  // Should be cache hit
                }
                sw.Stop();
                Console.WriteLine($"  Cache hits:    {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }
        }

        private static void InactiveRuleBenchmark()
        {
            Console.WriteLine("--- Inactive Rule Overhead ---");

            var activeRule = new Rule
            {
                Description = "Active",
                Expression = "customer.Age >= 18",
                IsActive = true
            };
            activeRule.Compile(_compiler, _parameters);

            var inactiveRule = new Rule
            {
                Description = "Inactive",
                Expression = "customer.Age >= 18",
                IsActive = false
            };
            inactiveRule.Compile(_compiler, _parameters);

            int iterations = 1000000;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                activeRule.Execute(_parameters);
            sw.Stop();
            Console.WriteLine($"  Active:   {sw.Elapsed.TotalMilliseconds:F2}ms");

            sw.Restart();
            for (int i = 0; i < iterations; i++)
                inactiveRule.Execute(_parameters);
            sw.Stop();
            Console.WriteLine($"  Inactive: {sw.Elapsed.TotalMilliseconds:F2}ms (overhead vs active: {sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
        }
    }

    public class ExecutionBenchmarkCustomer
    {
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public bool IsAdult { get; set; }
        public int Score { get; set; }
    }
}
