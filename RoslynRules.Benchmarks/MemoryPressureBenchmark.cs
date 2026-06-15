using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Stress tests and memory pressure benchmarks:
    /// - Many rules compiled
    /// - Many workflows
    /// - ALC accumulation without recycling
    /// - Large rule hierarchies
    /// - Long-running simulation
    /// </summary>
    public static class MemoryPressureBenchmark
    {
        private static readonly ExpressionCompiler _compiler = new();
        private static readonly RuleParameter[] _parameters = new[]
        {
            new RuleParameter("x", typeof(int), 42)
        };

        public static void Run()
        {
            Console.WriteLine("=== Memory Pressure Benchmark ===\n");

            ManyCompilationsBenchmark();
            Console.WriteLine();

            LargeHierarchyBenchmark();
            Console.WriteLine();

            AlcAccumulationBenchmark();
            Console.WriteLine();

            LongRunningSimulationBenchmark();
            Console.WriteLine();

            DelegateRetentionBenchmark();
        }

        private static void ManyCompilationsBenchmark()
        {
            Console.WriteLine("--- Many Compilations ---");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            int count = 500;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
            }
            sw.Stop();

            long memAfter = GC.GetTotalMemory(true);
            Console.WriteLine($"  {count} unique compiles: {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Memory before: {memBefore:N0} bytes");
            Console.WriteLine($"  Memory after:  {memAfter:N0} bytes");
            Console.WriteLine($"  Delta:         {memAfter - memBefore:N0} bytes ({(memAfter - memBefore) / count:F0} bytes/compile)");

            // Force unload
            compiler.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memAfterUnload = GC.GetTotalMemory(true);
            Console.WriteLine($"  After Unload(): {memAfterUnload:N0} bytes (recovered: {memAfter - memAfterUnload:N0} bytes)");
        }

        private static void LargeHierarchyBenchmark()
        {
            Console.WriteLine("--- Large Rule Hierarchy ---");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            var root = new Rule { Description = "Root", Expression = "true" };
            BuildHierarchy(root, depth: 0, maxDepth: 8, maxChildren: 5);

            long memAfter = GC.GetTotalMemory(true);
            int totalRules = CountRules(root);

            Console.WriteLine($"  Hierarchy: {totalRules} rules (depth 8, up to 5 children each)");
            Console.WriteLine($"  Memory: {memAfter - memBefore:N0} bytes ({(memAfter - memBefore) / totalRules:F0} bytes/rule)");

            // Compile and measure
            root.Compile(_compiler, _parameters);
            long memAfterCompile = GC.GetTotalMemory(true);
            Console.WriteLine($"  After compile: {memAfterCompile:N0} bytes (delta: {memAfterCompile - memAfter:N0} bytes)");

            static void BuildHierarchy(Rule parent, int depth, int maxDepth, int maxChildren)
            {
                if (depth >= maxDepth) return;
                int children = Math.Min(maxChildren, maxDepth - depth);
                for (int i = 0; i < children; i++)
                {
                    var child = new Rule
                    {
                        Description = $"Level {depth} Child {i}",
                        Expression = "x > 0"
                    };
                    parent.ChildRules.Add(child);
                    BuildHierarchy(child, depth + 1, maxDepth, maxChildren);
                }
            }

            static int CountRules(Rule rule)
            {
                int count = 1;
                foreach (var child in rule.ChildRules)
                    count += CountRules(child);
                return count;
            }
        }

        private static void AlcAccumulationBenchmark()
        {
            Console.WriteLine("--- ALC Accumulation (Without Recycling) ---");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            int count = 200;

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                compiler.Compile<Func<int, bool>>($"x > {i} && x < {i + 1000}", new[] { "x" });
            }
            sw.Stop();

            long memAfter = GC.GetTotalMemory(true);
            Console.WriteLine($"  {count} compiles (no recycle): {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Memory delta: {memAfter - memBefore:N0} bytes");
            Console.WriteLine($"  Compile count: {compiler.CompileCount}");
            Console.WriteLine($"  Context: {compiler.CurrentContextName}");

            // Now recycle
            compiler.Unload();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memAfterUnload = GC.GetTotalMemory(true);
            Console.WriteLine($"  After Unload(): {memAfterUnload:N0} bytes (recovered: {memAfter - memAfterUnload:N0} bytes)");
            Console.WriteLine($"  Context after unload: {compiler.CurrentContextName}");
        }

        private static void LongRunningSimulationBenchmark()
        {
            Console.WriteLine("--- Long-Running Simulation ---");

            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 100);
            var expressions = new[]
            {
                "x > 0", "x < 100", "x % 2 == 0", "x % 3 == 0", "System.Math.Abs(x) > 10",
                "x * 2 + 1 > 50", "x / 2 < 25", "x.ToString().Length > 1"
            };

            int iterations = 1000;
            int totalCompiles = 0;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                var expr = expressions[i % expressions.Length];
                var variant = $"{expr} && x != {i}";
                compiler.Compile<Func<int, bool>>(variant, new[] { "x" });
                totalCompiles++;
            }

            sw.Stop();
            long memAfter = GC.GetTotalMemory(true);

            Console.WriteLine($"  {totalCompiles} compiles over {iterations} iterations");
            Console.WriteLine($"  Elapsed: {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Context switches: ~{totalCompiles / 100} (recycle every 100)");
            Console.WriteLine($"  Final memory: {memAfter:N0} bytes");
            Console.WriteLine($"  Final context: {compiler.CurrentContextName}");
        }

        private static void DelegateRetentionBenchmark()
        {
            Console.WriteLine("--- Delegate Retention After Rule Discard ---");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            var delegates = new List<Delegate>();
            int count = 500;

            // Create and retain delegates
            for (int i = 0; i < count; i++)
            {
                var rule = new Rule
                {
                    Description = $"Rule {i}",
                    Expression = $"x > {i}"
                };
                rule.Compile(_compiler, _parameters);
                // Simulate retaining only the delegate
                var result = rule.Execute(_parameters);
                delegates.Add(() => result); // dummy retention
            }

            long memAfter = GC.GetTotalMemory(true);
            Console.WriteLine($"  {count} rules compiled and delegates retained");
            Console.WriteLine($"  Memory delta: {memAfter - memBefore:N0} bytes");

            // Clear and GC
            delegates.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memAfterGC = GC.GetTotalMemory(true);
            Console.WriteLine($"  After clearing delegates + GC: {memAfterGC:N0} bytes (recovered: {memAfter - memAfterGC:N0} bytes)");
        }
    }
}
