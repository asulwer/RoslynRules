using FluentAssertions;
using RoslynRules.Compiler;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RoslynRules.Tests.Compiler
{
    /// <summary>
    /// Direct unit tests for ExpressionCompiler — not via Rule.Compile().
    /// Covers Compile<T>(), Unload(), cache behavior, ALC recycling, and diagnostics.
    /// </summary>
    public class ExpressionCompilerTests
    {
        [Fact]
        public void Compile_FuncIntInt_ReturnsCorrectDelegate()
        {
            var compiler = new ExpressionCompiler();
            var del = compiler.Compile<Func<int, int>>("x * 2", new[] { "x" });

            del(5).Should().Be(10);
            del(3).Should().Be(6);
        }

        [Fact]
        public void Compile_FuncStringBool_ReturnsCorrectDelegate()
        {
            var compiler = new ExpressionCompiler();
            var del = compiler.Compile<Func<string, bool>>(
                "!string.IsNullOrEmpty(name)", new[] { "name" });

            del("hello").Should().BeTrue();
            del("").Should().BeFalse();
        }

        [Fact]
        public void Compile_ActionString_ReturnsCorrectDelegate()
        {
            var compiler = new ExpressionCompiler();
            var del = compiler.Compile<Action<string>>(
                "_ = value.Length", new[] { "value" });

            // Action has no return — just verify it doesn't throw
            var act = () => del("test");
            act.Should().NotThrow();
        }

        [Fact]
        public void Compile_FuncTwoParams_ReturnsCorrectDelegate()
        {
            var compiler = new ExpressionCompiler();
            var del = compiler.Compile<Func<int, int, int>>(
                "a + b", new[] { "a", "b" });

            del(3, 4).Should().Be(7);
        }

        [Fact]
        public void Compile_WithNamespaces_UsesAdditionalNamespaces()
        {
            var compiler = new ExpressionCompiler();
            var del = compiler.Compile<Func<int, bool>>(
                "System.Linq.Enumerable.Range(0, x).Any()",
                new[] { "x" },
                additionalNamespaces: new[] { "System.Linq" });

            del(5).Should().BeTrue();
            del(0).Should().BeFalse();
        }

        [Fact]
        public void Compile_SameExpression_ReturnsCachedDelegate()
        {
            var compiler = new ExpressionCompiler();

            var del1 = compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            var del2 = compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });

            // Same cached delegate — same reference
            del1.Should().BeSameAs(del2);
        }

        [Fact]
        public void Compile_DifferentExpressions_ReturnsDifferentDelegates()
        {
            var compiler = new ExpressionCompiler();

            var del1 = compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            var del2 = compiler.Compile<Func<int, int>>("x + 2", new[] { "x" });

            del1.Should().NotBeSameAs(del2);
            del1(5).Should().Be(6);
            del2(5).Should().Be(7);
        }

        [Fact]
        public void CompileCount_IncrementsOnNewCompilation()
        {
            var compiler = new ExpressionCompiler();
            compiler.CompileCount.Should().Be(0);

            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.CompileCount.Should().Be(1);

            compiler.Compile<Func<int, int>>("x + 2", new[] { "x" });
            compiler.CompileCount.Should().Be(2);
        }

        [Fact]
        public void CompileCount_DoesNotIncrementOnCacheHit()
        {
            var compiler = new ExpressionCompiler();

            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.CompileCount.Should().Be(1);

            // Same expression — cache hit, no recompile
            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.CompileCount.Should().Be(1);
        }

        [Fact]
        public void Unload_ClearsCache()
        {
            var compiler = new ExpressionCompiler();
            var del1 = compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });

            compiler.Unload();

            // After unload, should be a new delegate (not same reference)
            var del2 = compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            del1.Should().NotBeSameAs(del2);
        }

        [Fact]
        public void Unload_ResetsCompileCount()
        {
            var compiler = new ExpressionCompiler();
            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.CompileCount.Should().Be(1);

            compiler.Unload();
            compiler.CompileCount.Should().Be(0);
        }

        [Fact]
        public void CurrentContextName_ChangesAfterUnload()
        {
            var compiler = new ExpressionCompiler();
            var name1 = compiler.CurrentContextName;

            compiler.Unload();
            var name2 = compiler.CurrentContextName;

            name1.Should().NotBe(name2);
        }

        [Fact]
        public void Compile_InvalidExpression_Throws()
        {
            var compiler = new ExpressionCompiler();
            var act = () => compiler.Compile<Func<int, int>>(
                "not_valid_csharp_!!!", new[] { "x" });

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Compile_AfterUnload_WorksNormally()
        {
            var compiler = new ExpressionCompiler();
            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.Unload();

            var del = compiler.Compile<Func<int, int>>("x * 3", new[] { "x" });
            del(4).Should().Be(12);
        }

        [Fact]
        public void Compile_WithReferenceProvider_UsesWhitelist()
        {
            var compiler = new ExpressionCompiler();
            var provider = new AssemblyReferenceProvider();

            // System.Linq is in default whitelist — should work
            var del = compiler.Compile<Func<int, bool>>(
                "System.Linq.Enumerable.Range(0, x).Any()",
                new[] { "x" },
                referenceProvider: provider);

            del(5).Should().BeTrue();
        }

        [Fact]
        public void Compile_TwoCompilers_AreIndependent()
        {
            var compiler1 = new ExpressionCompiler();
            var compiler2 = new ExpressionCompiler();

            var del1 = compiler1.Compile<Func<int, int>>("x + 1", new[] { "x" });
            var del2 = compiler2.Compile<Func<int, int>>("x + 1", new[] { "x" });

            // Different compilers = different caches = different delegates
            del1.Should().NotBeSameAs(del2);
        }

        [Fact]
        public void RecycleAtMaxCompiles_CreatesNewContext()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 2);
            var name1 = compiler.CurrentContextName;

            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.Compile<Func<int, int>>("x + 2", new[] { "x" });

            // Third compile triggers recycle
            compiler.Compile<Func<int, int>>("x + 3", new[] { "x" });

            var name2 = compiler.CurrentContextName;
            name1.Should().NotBe(name2);
        }

        [Fact]
        public void CompileCount_AfterRecycle_ContinuesAccumulating()
        {
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 2);

            compiler.Compile<Func<int, int>>("x + 1", new[] { "x" });
            compiler.CompileCount.Should().Be(1);

            compiler.Compile<Func<int, int>>("x + 2", new[] { "x" });
            compiler.CompileCount.Should().Be(2);

            // Third compile triggers ALC recycle but count continues
            compiler.Compile<Func<int, int>>("x + 3", new[] { "x" });
            compiler.CompileCount.Should().Be(3);
        }
    }
}
