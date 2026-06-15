using RoslynRules.Compiler;
using RoslynRules.Models;
using RoslynRules.Templates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Benchmarks for RuleTemplate performance:
    /// - Template instantiation
    /// - Placeholder substitution
    /// - Multiple placeholders
    /// - Type vs Value placeholders
    /// </summary>
    public static class RuleTemplateBenchmark
    {
        private static readonly ExpressionCompiler _compiler = new();
        private static readonly RuleParameter[] _parameters = new[]
        {
            new RuleParameter("customer", typeof(ExecutionBenchmarkCustomer), new ExecutionBenchmarkCustomer { Age = 25 })
        };

        public static void Run()
        {
            Console.WriteLine("=== RuleTemplate Benchmark ===\n");

            TemplateInstantiationBenchmark();
            Console.WriteLine();

            PlaceholderCountBenchmark();
            Console.WriteLine();

            PlaceholderTypeBenchmark();
            Console.WriteLine();

            TemplateReuseBenchmark();
        }

        private static void TemplateInstantiationBenchmark()
        {
            Console.WriteLine("--- Template Instantiation ---");

            // Simple value placeholder
            {
                var template = new RuleTemplate
                {
                    Description = "Age threshold",
                    Expression = "customer.Age >= {minAge}"
                };
                template.Placeholders.Add("minAge", PlaceholderKind.Value);

                int iterations = 1000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var values = new Dictionary<string, object> { ["minAge"] = 18 + (i % 50) };
                    template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                }
                sw.Stop();
                Console.WriteLine($"  Value placeholder (1K inst):   {sw.Elapsed.TotalMilliseconds:F2}ms ({sw.Elapsed.TotalMilliseconds / iterations:F2}ms/inst)");
            }

            // Type placeholder
            {
                var template = new RuleTemplate
                {
                    Description = "Type placeholder",
                    Expression = "customer is {TType}"
                };
                template.Placeholders.Add("TType", PlaceholderKind.Type);

                int iterations = 100;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var values = new Dictionary<string, object> { ["TType"] = typeof(ExecutionBenchmarkCustomer) };
                    template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                }
                sw.Stop();
                Console.WriteLine($"  Type placeholder (100 inst):   {sw.Elapsed.TotalMilliseconds:F2}ms ({sw.Elapsed.TotalMilliseconds / iterations:F2}ms/inst)");
            }
        }

        private static void PlaceholderCountBenchmark()
        {
            Console.WriteLine("--- Placeholder Count Impact ---");

            foreach (var count in new[] { 1, 2, 5, 10 })
            {
                var template = new RuleTemplate
                {
                    Description = $"{count} placeholders"
                };

                var exprParts = new List<string>();
                var values = new Dictionary<string, object>();
                for (int i = 0; i < count; i++)
                {
                    var name = $"p{i}";
                    exprParts.Add($"customer.Age >= {{{name}}}");
                    template.Placeholders.Add(name, PlaceholderKind.Value);
                    values[name] = i * 10;
                }
                template.Expression = string.Join(" && ", exprParts);

                int iterations = 500;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                }
                sw.Stop();

                Console.WriteLine($"  {count} placeholders: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations} inst ({sw.Elapsed.TotalMilliseconds / iterations:F2}ms/inst)");
            }
        }

        private static void PlaceholderTypeBenchmark()
        {
            Console.WriteLine("--- Placeholder Type Performance ---");

            // Value placeholder
            {
                var template = new RuleTemplate
                {
                    Description = "Value",
                    Expression = "customer.Age >= {threshold}"
                };
                template.Placeholders.Add("threshold", PlaceholderKind.Value);

                var values = new Dictionary<string, object> { ["threshold"] = 18 };

                int iterations = 500;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                sw.Stop();
                Console.WriteLine($"  Value:  {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations} inst");
            }

            // Type placeholder
            {
                var template = new RuleTemplate
                {
                    Description = "Type",
                    Expression = "customer is {T}"
                };
                template.Placeholders.Add("T", PlaceholderKind.Type);

                var values = new Dictionary<string, object> { ["T"] = typeof(ExecutionBenchmarkCustomer) };

                int iterations = 500;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                sw.Stop();
                Console.WriteLine($"  Type:   {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations} inst");
            }

            // Identifier placeholder
            {
                var template = new RuleTemplate
                {
                    Description = "Identifier",
                    Expression = "customer.{field} >= 18"
                };
                template.Placeholders.Add("field", PlaceholderKind.Identifier);

                var values = new Dictionary<string, object> { ["field"] = "Age" };

                int iterations = 500;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                sw.Stop();
                Console.WriteLine($"  Ident:  {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations} inst");
            }
        }

        private static void TemplateReuseBenchmark()
        {
            Console.WriteLine("--- Template Reuse ---");

            var template = new RuleTemplate
            {
                Description = "Reused template",
                Expression = "customer.Age >= {minAge} && customer.Age <= {maxAge}"
            };
            template.Placeholders.Add("minAge", PlaceholderKind.Value);
            template.Placeholders.Add("maxAge", PlaceholderKind.Value);

            int iterations = 1000;

            // Instantiate every time (no reuse)
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var values = new Dictionary<string, object>
                {
                    ["minAge"] = 18,
                    ["maxAge"] = 65
                };
                var rule = template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());
                rule.Execute(_parameters);
            }
            sw.Stop();
            Console.WriteLine($"  Instantiate every time: {sw.Elapsed.TotalMilliseconds:F2}ms");

            // Instantiate once, execute many
            {
                var values = new Dictionary<string, object>
                {
                    ["minAge"] = 18,
                    ["maxAge"] = 65
                };
                var rule = template.Instantiate(values, _compiler, _parameters, Array.Empty<string>());

                sw.Restart();
                for (int i = 0; i < iterations; i++)
                {
                    rule.Execute(_parameters);
                }
                sw.Stop();
                Console.WriteLine($"  Instantiate once:     {sw.Elapsed.TotalMilliseconds:F2}ms");
            }
        }
    }

    public class TemplateBenchmarkCustomer
    {
        public int Age { get; set; }
    }
}
