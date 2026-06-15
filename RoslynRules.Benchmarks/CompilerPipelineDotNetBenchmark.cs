using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// BenchmarkDotNet benchmarks for the Roslyn compiler pipeline.
    /// Measures compilation time, delegate invocation, cache behavior, and ALC recycling.
    /// </summary>
    [MemoryDiagnoser]
    [RankColumn]
    public class CompilerPipelineDotNetBenchmark
    {
        private ExpressionCompiler _compiler = null!;
        private ExpressionCompiler _freshCompiler = null!;
        private List<string> _expressions = null!;

        [GlobalSetup]
        public void Setup()
        {
            _compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            _freshCompiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            _expressions = new List<string>
            {
                "x > 0",
                "x < 100",
                "x % 2 == 0",
                "x * 2 + 1 > 100",
                "System.Math.Abs(x) > 10"
            };
        }

        // ==================== COLD START ====================

        [Benchmark(Description = "Cold start: simple expression")]
        public void ColdStart_Simple()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            compiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
        }

        [Benchmark(Description = "Cold start: arithmetic expression")]
        public void ColdStart_Arithmetic()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            compiler.Compile<Func<int, bool>>("x * 2 + 1 > 100", new[] { "x" });
        }

        [Benchmark(Description = "Cold start: Math call")]
        public void ColdStart_Math()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            compiler.Compile<Func<double, bool>>("System.Math.Pow(x, 2) > 1000", new[] { "x" });
        }

        // ==================== WARM CACHE ====================

        [Benchmark(Description = "Warm cache: cached compile")]
        public void WarmCache_CachedCompile()
        {
            _compiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
        }

        [Benchmark(Description = "Warm cache: cache hit (10k)", OperationsPerInvoke = 10000)]
        public void WarmCache_10K_Hits()
        {
            for (int i = 0; i < 10000; i++)
            {
                _compiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
            }
        }

        // ==================== EXPRESSION COMPLEXITY ====================

        [Benchmark(Description = "Complexity: simple (1 condition)")]
        public void Complexity_Simple()
        {
            _freshCompiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
        }

        [Benchmark(Description = "Complexity: 2 conditions")]
        public void Complexity_TwoConditions()
        {
            _freshCompiler.Compile<Func<int, bool>>("x > 0 && x < 100", new[] { "x" });
        }

        [Benchmark(Description = "Complexity: 4 conditions")]
        public void Complexity_FourConditions()
        {
            _freshCompiler.Compile<Func<int, bool>>("x > 0 && x < 100 && x % 2 == 0 && x != 50", new[] { "x" });
        }

        [Benchmark(Description = "Complexity: nested ternary")]
        public void Complexity_NestedTernary()
        {
            _freshCompiler.Compile<Func<int, string>>("x > 0 ? (x > 50 ? \"high\" : \"medium\") : \"low\"", new[] { "x" });
        }

        // ==================== DELEGATE INVOCATION ====================

        private Func<int, bool>? _simpleDel;
        private Func<int, bool>? _arithmeticDel;
        private Func<double, bool>? _mathDel;
        private Func<string, bool>? _stringDel;

        [IterationSetup(Target = nameof(Invocation_Simple))]
        public void SetupSimpleInvocation()
        {
            _simpleDel = _compiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
        }

        [Benchmark(Description = "Invocation: simple compare", OperationsPerInvoke = 100000)]
        public void Invocation_Simple()
        {
            for (int i = 0; i < 100000; i++)
                _simpleDel!(42);
        }

        [IterationSetup(Target = nameof(Invocation_Arithmetic))]
        public void SetupArithmeticInvocation()
        {
            _arithmeticDel = _compiler.Compile<Func<int, bool>>("x * 2 + 1 > 100", new[] { "x" });
        }

        [Benchmark(Description = "Invocation: arithmetic", OperationsPerInvoke = 100000)]
        public void Invocation_Arithmetic()
        {
            for (int i = 0; i < 100000; i++)
                _arithmeticDel!(100);
        }

        [IterationSetup(Target = nameof(Invocation_Math))]
        public void SetupMathInvocation()
        {
            _mathDel = _compiler.Compile<Func<double, bool>>("System.Math.Pow(x, 2) > 1000", new[] { "x" });
        }

        [Benchmark(Description = "Invocation: Math.Pow", OperationsPerInvoke = 100000)]
        public void Invocation_Math()
        {
            for (int i = 0; i < 100000; i++)
                _mathDel!(35.0);
        }

        [IterationSetup(Target = nameof(Invocation_String))]
        public void SetupStringInvocation()
        {
            _stringDel = _compiler.Compile<Func<string, bool>>("s.Length > 5", new[] { "s" });
        }

        [Benchmark(Description = "Invocation: String.Length", OperationsPerInvoke = 100000)]
        public void Invocation_String()
        {
            for (int i = 0; i < 100000; i++)
                _stringDel!("Hello World");
        }

        [Benchmark(Description = "Invocation: hand-coded baseline", OperationsPerInvoke = 100000)]
        public void Invocation_HandCodedBaseline()
        {
            Func<int, bool> handCoded = x => x > 0;
            for (int i = 0; i < 100000; i++)
                handCoded(42);
        }

        // ==================== ALC RECYCLING ====================

        [Benchmark(Description = "ALC: no recycling (100 unique)")]
        public void Alc_NoRecycling()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            for (int i = 0; i < 100; i++)
            {
                compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
            }
        }

        [Benchmark(Description = "ALC: recycle at 50")]
        public void Alc_RecycleAt50()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 50);
            for (int i = 0; i < 100; i++)
            {
                compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
            }
        }

        [Benchmark(Description = "ALC: recycle at 10")]
        public void Alc_RecycleAt10()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 10);
            for (int i = 0; i < 100; i++)
            {
                compiler.Compile<Func<int, bool>>($"x > {i}", new[] { "x" });
            }
        }

        // ==================== ASYNC ====================

        [Benchmark(Description = "Async: compile sync delegate")]
        public void Async_CompileSync()
        {
            _freshCompiler.Compile<Func<int, bool>>("x > 0", new[] { "x" });
        }

        [Benchmark(Description = "Async: compile async delegate")]
        public void Async_CompileAsync()
        {
            _freshCompiler.Compile<Func<int, Task<bool>>>("await Task.FromResult(x > 0)", new[] { "x" });
        }

        // ==================== THROUGHPUT ====================

        [Benchmark(Description = "Throughput: rotating expressions (1000)", OperationsPerInvoke = 1000)]
        public void Throughput_RotatingExpressions()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            for (int i = 0; i < 1000; i++)
            {
                var expr = _expressions[i % _expressions.Count];
                compiler.Compile<Func<int, object?>>(expr, new[] { "x" });
            }
        }
    }
}
