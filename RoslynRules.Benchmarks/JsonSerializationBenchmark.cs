using RoslynRules.Json;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Benchmarks for JSON serialization performance:
    /// - Rule serialization/deserialization
    /// - Workflow serialization/deserialization
    /// - Round-trip integrity
    /// - Serialize vs Deserialize comparison
    /// - Different JSON options
    /// </summary>
    public static class JsonSerializationBenchmark
    {
        public static void Run()
        {
            Console.WriteLine("=== JSON Serialization Benchmark ===\n");

            RuleSerializationBenchmark();
            Console.WriteLine();

            WorkflowSerializationBenchmark();
            Console.WriteLine();

            RoundTripBenchmark();
            Console.WriteLine();

            ComplexWorkflowBenchmark();
            Console.WriteLine();

            JsonOptionsBenchmark();
        }

        private static void RuleSerializationBenchmark()
        {
            Console.WriteLine("--- Rule Serialization ---");

            var rule = new Rule
            {
                Id = Guid.NewGuid(),
                Description = "Test rule",
                Expression = "customer.Age >= 18",
                Action = "customer.IsAdult = true",
                Priority = 100,
                IsActive = true
            };

            int iterations = 10000;

            // Serialize
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                JsonRuleLoader.Serialize(rule);
            }
            sw.Stop();
            Console.WriteLine($"  Serialize:  {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");

            // Deserialize
            var json = JsonRuleLoader.Serialize(rule);
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                JsonRuleLoader.DeserializeRule(json);
            }
            sw.Stop();
            Console.WriteLine($"  Deserialize: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");

            // JSON size
            Console.WriteLine($"  JSON size: {json.Length} chars");
        }

        private static void WorkflowSerializationBenchmark()
        {
            Console.WriteLine("--- Workflow Serialization ---");

            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Description = "Test workflow",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule 1", Expression = "x > 0" },
                    new Rule { Description = "Rule 2", Expression = "x < 100" },
                    new Rule { Description = "Rule 3", Expression = "x % 2 == 0" }
                }
            };

            int iterations = 10000;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                JsonRuleLoader.Serialize(workflow);
            }
            sw.Stop();
            Console.WriteLine($"  Serialize:  {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");

            var json = JsonRuleLoader.Serialize(workflow);
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                JsonRuleLoader.DeserializeWorkflow(json);
            }
            sw.Stop();
            Console.WriteLine($"  Deserialize: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");

            Console.WriteLine($"  JSON size: {json.Length} chars");
        }

        private static void RoundTripBenchmark()
        {
            Console.WriteLine("--- Round-Trip Integrity ---");

            var original = new Workflow
            {
                Id = Guid.NewGuid(),
                Description = "Round-trip test",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Id = Guid.NewGuid(),
                        Description = "Parent",
                        Expression = "true",
                        Priority = 50,
                        IsActive = true,
                        ChildRules = new List<Rule>
                        {
                            new Rule
                            {
                                Id = Guid.NewGuid(),
                                Description = "Child",
                                Expression = "x > 0"
                            }
                        }
                    }
                }
            };

            int iterations = 1000;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var json = JsonRuleLoader.Serialize(original);
                var restored = JsonRuleLoader.DeserializeWorkflow(json);
            }
            sw.Stop();

            Console.WriteLine($"  {iterations:N0} round-trips: {sw.Elapsed.TotalMilliseconds:F2}ms ({sw.Elapsed.TotalMilliseconds / iterations:F2}ms/round-trip)");

            // Verify integrity
            var jsonFinal = JsonRuleLoader.Serialize(original);
            var final = JsonRuleLoader.DeserializeWorkflow(jsonFinal);
            Console.WriteLine($"  Original rules: {original.Rules.Count}, Restored: {final.Rules.Count}");
            Console.WriteLine($"  Original child rules: {original.Rules[0].ChildRules.Count}, Restored: {final.Rules[0].ChildRules.Count}");
            Console.WriteLine($"  Priority preserved: {original.Rules[0].Priority == final.Rules[0].Priority}");
        }

        private static void ComplexWorkflowBenchmark()
        {
            Console.WriteLine("--- Complex Workflow (Many Rules) ---");

            foreach (var ruleCount in new[] { 10, 50, 100 })
            {
                var workflow = new Workflow
                {
                    Id = Guid.NewGuid(),
                    Description = $"Complex ({ruleCount} rules)"
                };

                for (int i = 0; i < ruleCount; i++)
                {
                    workflow.Rules.Add(new Rule
                    {
                        Id = Guid.NewGuid(),
                        Description = $"Rule {i}",
                        Expression = $"x > {i}",
                        Priority = i,
                        IsActive = i % 2 == 0
                    });
                }

                int iterations = 10000 / Math.Max(ruleCount / 10, 1);

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var json = JsonRuleLoader.Serialize(workflow);
                }
                sw.Stop();

                var jsonSize = JsonRuleLoader.Serialize(workflow).Length;
                Console.WriteLine($"  {ruleCount} rules: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations} serializes ({sw.Elapsed.TotalMilliseconds / iterations:F2}ms/serialize), JSON: {jsonSize} chars");
            }
        }

        private static void JsonOptionsBenchmark()
        {
            Console.WriteLine("--- JSON Options Comparison ---");

            var workflow = new Workflow
            {
                Id = Guid.NewGuid(),
                Description = "Options test",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule 1", Expression = "x > 0" }
                }
            };

            int iterations = 10000;

            // Default options
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    JsonRuleLoader.Serialize(workflow);
                sw.Stop();
                Console.WriteLine($"  Default options:     {sw.Elapsed.TotalMilliseconds:F2}ms");
            }

            // Compact options (no indent)
            {
                var options = new JsonSerializerOptions(JsonRuleLoader.DefaultOptions)
                {
                    WriteIndented = false
                };
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    JsonRuleLoader.Serialize(workflow, options);
                sw.Stop();
                Console.WriteLine($"  Compact (no indent): {sw.Elapsed.TotalMilliseconds:F2}ms");
            }

            // CamelCase vs no naming policy
            {
                var options = new JsonSerializerOptions(JsonRuleLoader.DefaultOptions)
                {
                    PropertyNamingPolicy = null
                };
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    JsonRuleLoader.Serialize(workflow, options);
                sw.Stop();
                Console.WriteLine($"  No naming policy:    {sw.Elapsed.TotalMilliseconds:F2}ms");
            }
        }
    }
}
