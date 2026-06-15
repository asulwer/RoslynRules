using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// Benchmarks for RuleCache performance:
    /// - Cache hit vs miss
    /// - Cache with different durations
    /// - Cache eviction
    /// - Memory overhead
    /// </summary>
    public static class CacheBenchmark
    {
        private static readonly ExpressionCompiler _compiler = new();
        private static readonly RuleParameter[] _parameters = new[]
        {
            new RuleParameter("x", typeof(int), 42)
        };

        public static void Run()
        {
            Console.WriteLine("=== Cache Benchmark ===\n");

            CacheHitVsMissBenchmark();
            Console.WriteLine();

            CacheDurationBenchmark();
            Console.WriteLine();

            CacheEvictionBenchmark();
            Console.WriteLine();

            CacheMemoryBenchmark();
        }

        private static void CacheHitVsMissBenchmark()
        {
            Console.WriteLine("--- Cache Hit vs Miss ---");

            var rule = new Rule
            {
                Description = "Cache test",
                Expression = "x > 0",
                CacheDuration = TimeSpan.FromMinutes(5)
            };
            rule.Compile(_compiler, _parameters);

            // First call (miss)
            var sw = Stopwatch.StartNew();
            rule.Execute(_parameters);
            sw.Stop();
            var missMs = sw.Elapsed.TotalMilliseconds;

            // Second call (hit)
            sw.Restart();
            rule.Execute(_parameters);
            sw.Stop();
            var hitMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"  Cache miss: {missMs:F3}ms");
            Console.WriteLine($"  Cache hit:  {hitMs:F3}ms");
            Console.WriteLine($"  Speedup:    {missMs / hitMs:F1}x");

            // Many hits
            int iterations = 1000000;
            sw.Restart();
            for (int i = 0; i < iterations; i++)
                rule.Execute(_parameters);
            sw.Stop();
            Console.WriteLine($"  1M cache hits: {sw.Elapsed.TotalMilliseconds:F2}ms ({sw.Elapsed.TotalMilliseconds / iterations * 1000:F2} ns/call)");
        }

        private static void CacheDurationBenchmark()
        {
            Console.WriteLine("--- Cache Duration Impact ---");

            foreach (var duration in new[] { 1, 5, 10, 60 })  // seconds
            {
                var rule = new Rule
                {
                    Description = $"Cache {duration}s",
                    Expression = "x > 0",
                    CacheDuration = TimeSpan.FromSeconds(duration)
                };
                rule.Compile(_compiler, _parameters);

                // Warm cache
                rule.Execute(_parameters);

                int iterations = 100000;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                    rule.Execute(_parameters);
                sw.Stop();

                Console.WriteLine($"  Duration {duration}s: {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations:N0} hits");
            }
        }

        private static void CacheEvictionBenchmark()
        {
            Console.WriteLine("--- Cache Eviction ---");

            var rule = new Rule
            {
                Description = "Short cache",
                Expression = "x > 0",
                CacheDuration = TimeSpan.FromMilliseconds(10)
            };
            rule.Compile(_compiler, _parameters);

            // Fill cache
            int fillCount = 10000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < fillCount; i++)
            {
                var param = new RuleParameter("x", typeof(int), i);
                rule.Execute(param);
            }
            sw.Stop();
            Console.WriteLine($"  Fill cache (10K unique params): {sw.Elapsed.TotalMilliseconds:F2}ms");

            // Wait for expiry
            System.Threading.Thread.Sleep(50);

            // Access again (all should be misses)
            sw.Restart();
            for (int i = 0; i < fillCount; i++)
            {
                var param = new RuleParameter("x", typeof(int), i);
                rule.Execute(param);
            }
            sw.Stop();
            Console.WriteLine($"  Re-access after expiry:         {sw.Elapsed.TotalMilliseconds:F2}ms");

            // ClearCache performance
            rule = new Rule
            {
                Description = "Clear test",
                Expression = "x > 0",
                CacheDuration = TimeSpan.FromMinutes(5)
            };
            rule.Compile(_compiler, _parameters);
            for (int i = 0; i < 10000; i++)
            {
                var param = new RuleParameter("x", typeof(int), i);
                rule.Execute(param);
            }

            sw.Restart();
            rule.ClearCache();
            sw.Stop();
            Console.WriteLine($"  ClearCache (10K entries):       {sw.Elapsed.TotalMilliseconds:F3}ms");
        }

        private static void CacheMemoryBenchmark()
        {
            Console.WriteLine("--- Cache Memory Overhead ---");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long baseline = GC.GetTotalMemory(true);

            var rule = new Rule
            {
                Description = "Memory test",
                Expression = "x > 0",
                CacheDuration = TimeSpan.FromMinutes(5)
            };
            rule.Compile(_compiler, _parameters);

            // Fill with unique entries
            int entries = 100000;
            for (int i = 0; i < entries; i++)
            {
                var param = new RuleParameter("x", typeof(int), i);
                rule.Execute(param);
            }

            long afterFill = GC.GetTotalMemory(true);
            GC.Collect();

            Console.WriteLine($"  Baseline memory:   {baseline:N0} bytes");
            Console.WriteLine($"  After {entries:N0} entries: {afterFill:N0} bytes");
            Console.WriteLine($"  Cache overhead:    {afterFill - baseline:N0} bytes ({(afterFill - baseline) / entries:F0} bytes/entry)");

            // Clear and measure
            rule.ClearCache();
            long afterClear = GC.GetTotalMemory(true);
            GC.Collect();
            Console.WriteLine($"  After ClearCache:  {afterClear:N0} bytes");
        }
    }
}
