using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Benchmarks the Roslyn expression compilation pipeline:
    /// - First-time compilation (cold start)
    /// - Cached compilation (warm)
    /// - Delegate invocation speed
    /// - ALC recycling impact
    /// - Complex vs simple expressions
    /// - Async expression compilation
    /// </summary>
    public static class CompilerPipelineBenchmark
    {
        public static void Run()
        {
            Console.WriteLine("=== Compiler Pipeline Benchmark ===\n");

            ColdStartBenchmark();
            Console.WriteLine();

            WarmCacheBenchmark();
            Console.WriteLine();

            DelegateInvocationBenchmark();
            Console.WriteLine();

            ExpressionComplexityBenchmark();
            Console.WriteLine();

            AsyncCompilationBenchmark();
            Console.WriteLine();

            AlcRecyclingBenchmark();
            Console.WriteLine();

            CompilationThroughputBenchmark();
        }

        private static void ColdStartBenchmark()
        {
            Console.WriteLine("--- Cold Start Compilation ---");
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            var param = new RuleParameter("x", typeof(int), 0);

            // Force fresh compiler for each test
            var expressions = new[]
            {
                "x > 0",
                "x == 42",
                "x % 2 == 0",
                "x >= 0 && x <= 100",
                "System.Math.Abs(x) > 10"
            };

            foreach (var expr in expressions)
            {
                var freshCompiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
                var sw = Stopwatch.StartNew();
                var del = freshCompiler.Compile<Func<int, bool>>(expr, new[] { "x" });
                sw.Stop();
                Console.WriteLine($"  '{expr}' = {sw.Elapsed.TotalMilliseconds:F2}ms (first compile)");
            }
        }

        private static void WarmCacheBenchmark()
        {
            Console.WriteLine("--- Warm Cache (Cached Compile) ---");
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            var expr = "x > 0";
            var paramNames = new[] { "x" };

            // First call warms the cache
            compiler.Compile<Func<int, bool>>(expr, paramNames);

            int iterations = 10000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                compiler.Compile<Func<int, bool>>(expr, paramNames);
            }
            sw.Stop();

            Console.WriteLine($"  {iterations:N0} cached compilations: {sw.Elapsed.TotalMilliseconds:F2}ms");
            Console.WriteLine($"  Average per call: {sw.Elapsed.TotalMilliseconds / iterations:F4}ms");
        }

        private static void DelegateInvocationBenchmark()
        {
            Console.WriteLine("--- Delegate Invocation Speed ---");
            var compiler = new ExpressionCompiler();
            var expressions = new Dictionary<string, string>
            {
                ["Simple compare"] = "x > 0",
                ["Arithmetic"] = "x * 2 + 1 > 100",
                ["Math call"] = "System.Math.Pow(x, 2) > 1000",
                ["String length"] = "s.Length > 5",
                ["Linq Any"] = "items.Any(i => i > 50)",
            };

            int iterations = 1000000;

            // Simple int compare
            {
                var del = compiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    del(42);
                sw.Stop();
                Console.WriteLine($"  Simple compare (int):      {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }

            // Arithmetic
            {
                var del = compiler.Compile<Func<int, bool>>("x * 2 + 1 > 100", new[] { "x" });
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    del(100);
                sw.Stop();
                Console.WriteLine($"  Arithmetic (int):          {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }

            // Math call
            {
                var del = compiler.Compile<Func<double, bool>>("System.Math.Pow(x, 2) > 1000", new[] { "x" });
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    del(35.0);
                sw.Stop();
                Console.WriteLine($"  Math.Pow (double):         {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }

            // String length
            {
                var del = compiler.Compile<Func<string, bool>>("s.Length > 5", new[] { "s" });
                var testString = "Hello World";
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    del(testString);
                sw.Stop();
                Console.WriteLine($"  String.Length:           {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }

            // LINQ Any
            {
                var del = compiler.Compile<Func<List<int>, bool>>("items.Any(i => i > 50)", new[] { "items" });
                var testList = new List<int> { 10, 20, 30, 40, 60, 70 };
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations / 10; i++)  // slower, fewer iterations
                    del(testList);
                sw.Stop();
                Console.WriteLine($"  LINQ Any (List<int>):     {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations / 10:N0} calls ({sw.Elapsed.TotalMilliseconds / (iterations / 10) * 1000:F2} ns/call)");
            }

            // Baseline: hand-coded for comparison
            {
                Func<int, bool> handCoded = x => x > 0;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    handCoded(42);
                sw.Stop();
                Console.WriteLine($"  Hand-coded baseline:       {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }
        }

        private static void ExpressionComplexityBenchmark()
        {
            Console.WriteLine("--- Expression Complexity vs Compile Time ---");
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            var param = new RuleParameter("x", typeof(int), 0);

            var complexities = new (string Name, string Expr, string[] Params)[]
            {
                ("Simple", "x > 0", new[] { "x" }),
                ("2 conditions", "x > 0 && x < 100", new[] { "x" }),
                ("4 conditions", "x > 0 && x < 100 && x % 2 == 0 && x != 50", new[] { "x" }),
                ("Math chain", "System.Math.Abs(System.Math.Sin(x) * System.Math.Cos(x)) > 0.5", new[] { "x" }),
                ("Nested ternary", "x > 0 ? (x > 50 ? \"high\" : \"medium\") : \"low\"", new[] { "x" }),
                ("String concat", "\"prefix_\" + x.ToString()", new[] { "x" }),
            };

            foreach (var (name, expr, paramNames) in complexities)
            {
                var freshCompiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
                var sw = Stopwatch.StartNew();
                var del = freshCompiler.Compile<Func<int, object?>>(expr, paramNames);
                sw.Stop();
                Console.WriteLine($"  {name,-15} {sw.Elapsed.TotalMilliseconds,8:F2}ms  ({expr.Length} chars)");
            }
        }

        private static void AsyncCompilationBenchmark()
        {
            Console.WriteLine("--- Async Expression Compilation ---");
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);

            // Sync version
            {
                var sw = Stopwatch.StartNew();
                var del = compiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
                sw.Stop();
                Console.WriteLine($"  Sync compile:   {sw.Elapsed.TotalMilliseconds:F2}ms");
            }

            // Async version
            {
                var sw = Stopwatch.StartNew();
                var del = compiler.Compile<Func<int, Task<bool>>>("await Task.FromResult(x > 0)", new[] { "x" });
                sw.Stop();
                Console.WriteLine($"  Async compile:  {sw.Elapsed.TotalMilliseconds:F2}ms");
            }

            // Async invocation timing
            {
                var del = compiler.Compile<Func<int, Task<bool>>>("await Task.FromResult(x > 0)", new[] { "x" });
                int iterations = 100000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var task = del(42);
                    // Don't await — just measure delegate invocation overhead
                }
                sw.Stop();
                Console.WriteLine($"  Async invoke:   {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} calls ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
            }
        }

        private static void AlcRecyclingBenchmark()
        {
            Console.WriteLine("--- ALC Recycling Impact ---");

            // No recycling
            {
                var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
                }
                sw.Stop();
                var memAfter = GC.GetTotalMemory(true);
                Console.WriteLine($"  No recycling (100 unique):     {sw.Elapsed.TotalMilliseconds:F2}ms, Memory: {memAfter:N0} bytes");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // With recycling at 50
            {
                var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 50);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
                }
                sw.Stop();
                var memAfter = GC.GetTotalMemory(true);
                Console.WriteLine($"  Recycle at 50 (100 unique):    {sw.Elapsed.TotalMilliseconds:F2}ms, Memory: {memAfter:N0} bytes");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // With recycling at 10
            {
                var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 10);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
                }
                sw.Stop();
                var memAfter = GC.GetTotalMemory(true);
                Console.WriteLine($"  Recycle at 10 (100 unique):    {sw.Elapsed.TotalMilliseconds:F2}ms, Memory: {memAfter:N0} bytes");
            }
        }

        private static void CompilationThroughputBenchmark()
        {
            Console.WriteLine("--- Compilation Throughput ---");
            var expressions = new[]
            {
                "x > 0",
                "x < 100",
                "x % 2 == 0",
                "x * 2 + 1",
                "System.Math.Abs(x)",
            };

            int iterations = 1000;

            // Single compiler, rotating expressions
            {
                var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    var expr = expressions[i % expressions.Length];
                    compiler.Compile<Func<int, object?>>(expr, new[] { "x" });
                }
                sw.Stop();
                Console.WriteLine($"  Single compiler, rotating:  {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} compiles ({sw.Elapsed.TotalMilliseconds / iterations:F2}ms/compile)");
                Console.WriteLine($"  Throughput: {iterations / sw.Elapsed.TotalSeconds:F0} compiles/sec");
            }

            // Fresh compiler each time (worst case)
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 50; i++)  // fewer iterations — too slow
                {
                    var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
                    var expr = expressions[i % expressions.Length];
                    compiler.Compile<Func<int, object?>>(expr, new[] { "x" });
                }
                sw.Stop();
                Console.WriteLine($"  Fresh compiler each time:   {sw.Elapsed.TotalMilliseconds:F2}ms for 50 compiles ({sw.Elapsed.TotalMilliseconds / 50:F2}ms/compile)");
            }
        }
    }
}
