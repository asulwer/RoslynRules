using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Benchmarks for Workflow execution performance:
    /// - Sequential vs parallel execution
    /// - Workflow with many rules
    /// - Dependency ordering overhead
    /// - Rule priority ordering
    /// </summary>
    public static class WorkflowBenchmark
    {
        private static readonly ExpressionCompiler _compiler = new();
        private static readonly RuleParameter[] _parameters = new[]
        {
            new RuleParameter("customer", typeof(ExecutionBenchmarkCustomer), new ExecutionBenchmarkCustomer { Age = 25, IsActive = true, Score = 75 })
        };

        public static void Run()
        {
            Console.WriteLine("=== Workflow Benchmark ===\n");

            SequentialVsParallelBenchmark();
            Console.WriteLine();

            ManyRulesBenchmark();
            Console.WriteLine();

            DependencyOrderingBenchmark();
            Console.WriteLine();

            PriorityOrderingBenchmark();
        }

        private static void SequentialVsParallelBenchmark()
        {
            Console.WriteLine("--- Sequential vs Parallel Execution ---");

            foreach (var ruleCount in new[] { 1, 5, 10, 20, 50 })
            {
                var workflow = new Workflow
                {
                    Description = $"{ruleCount} rules"
                };

                for (int i = 0; i < ruleCount; i++)
                {
                    workflow.Rules.Add(new Rule
                    {
                        Description = $"Rule {i}",
                        Expression = "customer.Age > 0"
                    });
                }

                workflow.Compile(_parameters);

                int iterations = 100000 / Math.Max(ruleCount, 1);

                // Sequential
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    workflow.Execute(_parameters);
                }
                sw.Stop();
                var seqMs = sw.Elapsed.TotalMilliseconds;

                // Parallel
                sw.Restart();
                for (int i = 0; i < iterations; i++)
                {
                    workflow.ExecuteParallel(_parameters);
                }
                sw.Stop();
                var parMs = sw.Elapsed.TotalMilliseconds;

                Console.WriteLine($"  {ruleCount} rules: Seq {seqMs,8:F2}ms / Par {parMs,8:F2}ms (ratio: {seqMs / parMs:F2}x, {iterations:N0} iterations)");
            }
        }

        private static void ManyRulesBenchmark()
        {
            Console.WriteLine("--- Workflow with Many Rules ---");

            foreach (var ruleCount in new[] { 10, 50, 100, 200 })
            {
                var workflow = new Workflow();
                for (int i = 0; i < ruleCount; i++)
                {
                    workflow.Rules.Add(new Rule
                    {
                        Description = $"Rule {i}",
                        Expression = "customer.Score > 0"
                    });
                }

                workflow.Compile(_parameters);

                int iterations = 50000 / Math.Max(ruleCount / 10, 1);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    workflow.Execute(_parameters);
                }
                sw.Stop();

                Console.WriteLine($"  {ruleCount} rules: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} executions ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }
        }

        private static void DependencyOrderingBenchmark()
        {
            Console.WriteLine("--- Dependency Ordering Overhead ---");

            // No dependencies
            {
                var workflow = new Workflow();
                for (int i = 0; i < 10; i++)
                {
                    workflow.Rules.Add(new Rule
                    {
                        Id = Guid.NewGuid(),
                        Description = $"Rule {i}",
                        Expression = "customer.Age > 0"
                    });
                }
                workflow.Compile(_parameters);

                int iterations = 50000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    workflow.Execute(_parameters);
                sw.Stop();
                Console.WriteLine($"  No dependencies:     {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls");
            }

            // Linear chain: rule 1 -> rule 2 -> rule 3 ...
            {
                var workflow = new Workflow();
                var prevId = Guid.NewGuid();
                workflow.Rules.Add(new Rule
                {
                    Id = prevId,
                    Description = "Rule 0",
                    Expression = "customer.Age > 0"
                });

                for (int i = 1; i < 10; i++)
                {
                    var id = Guid.NewGuid();
                    workflow.Rules.Add(new Rule
                    {
                        Id = id,
                        Description = $"Rule {i}",
                        Expression = "customer.Age > 0",
                        DependsOnRuleId = prevId
                    });
                    prevId = id;
                }
                workflow.Compile(_parameters);

                int iterations = 50000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    workflow.Execute(_parameters);
                sw.Stop();
                Console.WriteLine($"  Linear chain (10):   {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls");
            }

            // Star pattern: all depend on center
            {
                var workflow = new Workflow();
                var centerId = Guid.NewGuid();
                workflow.Rules.Add(new Rule
                {
                    Id = centerId,
                    Description = "Center",
                    Expression = "customer.Age > 0"
                });

                for (int i = 0; i < 9; i++)
                {
                    workflow.Rules.Add(new Rule
                    {
                        Id = Guid.NewGuid(),
                        Description = $"Dependent {i}",
                        Expression = "customer.Age > 0",
                        DependsOnRuleId = centerId
                    });
                }
                workflow.Compile(_parameters);

                int iterations = 50000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    workflow.Execute(_parameters);
                sw.Stop();
                Console.WriteLine($"  Star pattern (10):   {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls");
            }
        }

        private static void PriorityOrderingBenchmark()
        {
            Console.WriteLine("--- Priority Ordering ---");

            // Random priorities
            var workflow = new Workflow();
            var random = new Random(42);
            for (int i = 0; i < 20; i++)
            {
                workflow.Rules.Add(new Rule
                {
                    Description = $"Rule {i}",
                    Expression = "customer.Age > 0",
                    Priority = random.Next(-100, 100)
                });
            }
            workflow.Compile(_parameters);

            int iterations = 100000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                workflow.Execute(_parameters);
            sw.Stop();

            Console.WriteLine($"  20 rules with random priorities: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls");
        }
    }

    public class WorkflowBenchmarkCustomer
    {
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public int Score { get; set; }
    }
}
