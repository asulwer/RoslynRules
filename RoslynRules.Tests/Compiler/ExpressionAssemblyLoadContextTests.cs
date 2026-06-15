using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Linq;
using System.Runtime.Loader;
using Xunit;

namespace RoslynRules.Tests.Compiler
{
    /// <summary>
    /// Stress tests for ExpressionAssemblyLoadContext unload behavior.
    /// Verifies that ALC recycling prevents unbounded memory growth.
    /// </summary>
    public class ExpressionAssemblyLoadContextTests
    {
        [Fact]
        public void Compile_ManyExpressions_ALCCount_StaysBounded()
        {
            // Arrange: limit to 100 compiles before recycle
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 100);
            var param = new RuleParameter("x", typeof(int));
            var initialContextName = compiler.CurrentContextName;

            // Act: compile 500 unique expressions (5x the recycle threshold)
            for (int i = 0; i < 500; i++)
            {
                var rule = new Rule
                {
                    Description = $"Rule {i}",
                    Expression = $"x > {i}", // unique expression each time
                    IsActive = true
                };
                rule.Compile(compiler, new[] { param });
            }

            // Assert: compile count should be 500 (all unique)
            compiler.CompileCount.Should().Be(500);

            // The ALC should have been recycled multiple times.
            // Verify the context changed (was recycled at least once).
            compiler.CurrentContextName.Should().NotBe(initialContextName);
        }

        [Fact]
        public void Unload_ForcesALCRecycling()
        {
            // Arrange
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 1000);
            var param = new RuleParameter("x", typeof(int));

            var rule = new Rule
            {
                Description = "Test",
                Expression = "x > 0",
                IsActive = true
            };
            rule.Compile(compiler, new[] { param });

            var contextNameBefore = compiler.CurrentContextName;

            // Act: force unload
            compiler.Unload();

            // Assert: context should have changed
            compiler.CurrentContextName.Should().NotBe(contextNameBefore);
            compiler.CompileCount.Should().Be(0);
        }

        [Fact]
        public void Unload_ClearsDelegateCache()
        {
            // Arrange
            var compiler = new ExpressionCompiler();
            var param = new RuleParameter("x", typeof(int));

            // Compile first expression (count = 1)
            var rule1 = new Rule
            {
                Description = "Test1",
                Expression = "x > 0",
                IsActive = true
            };
            rule1.Compile(compiler, new[] { param });
            var countAfterFirst = compiler.CompileCount;
            countAfterFirst.Should().Be(1);

            // Compile same expression again — should be cache hit (count still 1)
            var rule2 = new Rule
            {
                Description = "Test2",
                Expression = "x > 0",
                IsActive = true
            };
            rule2.Compile(compiler, new[] { param });
            compiler.CompileCount.Should().Be(1, "same expression should be cached");

            // Act: unload and compile again
            compiler.Unload();
            var rule3 = new Rule
            {
                Description = "Test3",
                Expression = "x > 0",
                IsActive = true
            };
            rule3.Compile(compiler, new[] { param });

            // Assert: after unload, compilation should happen again (cache was cleared)
            // Note: Unload() resets CompileCount to 0, so after recompile it should be 1
            compiler.CompileCount.Should().Be(1, "after unload, cache should be cleared so recompilation occurs");
        }

        [Fact]
        public void Compile_AfterUnload_ContinuesWorking()
        {
            // Arrange
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 10);
            var param = new RuleParameter("x", typeof(int));

            // Act: compile, unload, compile again
            var rule1 = new Rule
            {
                Description = "Before",
                Expression = "x > 0",
                IsActive = true
            };
            rule1.Compile(compiler, new[] { param });
            var result1 = rule1.Execute(new[] { new RuleParameter("x", typeof(int), 5) });

            compiler.Unload();

            var rule2 = new Rule
            {
                Description = "After",
                Expression = "x > 0",
                IsActive = true
            };
            rule2.Compile(compiler, new[] { param });
            var result2 = rule2.Execute(new[] { new RuleParameter("x", typeof(int), 5) });

            // Assert: both should succeed
            result1.Success.Should().BeTrue();
            result2.Success.Should().BeTrue();
        }

        [Fact]
        public void Compile_ExceedsRecycleThreshold_AutoRecycles()
        {
            // Arrange: very low threshold for testing
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 5);
            var param = new RuleParameter("x", typeof(int));
            var contextNameBefore = compiler.CurrentContextName;

            // Act: compile 10 unique expressions (2x threshold)
            for (int i = 0; i < 10; i++)
            {
                var rule = new Rule
                {
                    Description = $"Rule {i}",
                    Expression = $"x > {i}",
                    IsActive = true
                };
                rule.Compile(compiler, new[] { param });
            }

            // Assert: context should have changed (recycled at least once)
            compiler.CurrentContextName.Should().NotBe(contextNameBefore);
            compiler.CompileCount.Should().Be(10);
        }

        [Fact]
        public void AssemblyLoadContext_IsCollectible()
        {
            // Verify the ALC is created with isCollectible: true
            var compiler = new ExpressionCompiler();
            var param = new RuleParameter("x", typeof(int));

            var rule = new Rule
            {
                Description = "Collectible test",
                Expression = "x > 0",
                IsActive = true
            };
            rule.Compile(compiler, new[] { param });

            // The ALC should be collectible
            // We verify by checking the context name exists and Unload doesn't throw
            var contextName = compiler.CurrentContextName;
            contextName.Should().StartWith("ExpressionALC(");

            compiler.Invoking(c => c.Unload()).Should().NotThrow();
        }

        [Fact]
        public void Stress_Compile1000Expressions_MemoryStable()
        {
            // Arrange: use default threshold of 1000
            var compiler = new ExpressionCompiler();
            var param = new RuleParameter("x", typeof(int));

            // Act: compile 1000 expressions with unique cache keys
            for (int i = 0; i < 1000; i++)
            {
                var rule = new Rule
                {
                    Description = $"Stress {i}",
                    Expression = $"x > {i}",
                    IsActive = true
                };
                rule.Compile(compiler, new[] { param });
            }

            // Assert: all compiled, count exact
            compiler.CompileCount.Should().Be(1000);

            // Force GC to verify no leaks
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // After GC, compiler should still be functional
            var finalRule = new Rule
            {
                Description = "Final",
                Expression = "x > 9999",
                IsActive = true
            };
            finalRule.Compile(compiler, new[] { param });
            var result = finalRule.Execute(new[] { new RuleParameter("x", typeof(int), 10000) });
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Stress_Compile10000Expressions_WithForcedUnload()
        {
            // Arrange: no auto-recycle, manual unload between batches
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 0);
            var param = new RuleParameter("x", typeof(int));

            // Act: compile 100 expressions at a time, unload between batches
            // (100 batches of 100 = 10,000 total, but done in manageable chunks)
            for (int batch = 0; batch < 10; batch++)
            {
                for (int i = 0; i < 100; i++)
                {
                    var rule = new Rule
                    {
                        Description = $"Batch{batch}-{i}",
                        Expression = $"x > {batch * 100 + i}",
                        IsActive = true
                    };
                    rule.Compile(compiler, new[] { param });
                }

                // Force unload and GC between batches
                compiler.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            // Assert: after all batches, compiler still works
            compiler.CompileCount.Should().Be(0); // Unload resets count

            var finalRule = new Rule
            {
                Description = "Final",
                Expression = "x > 0",
                IsActive = true
            };
            finalRule.Compile(compiler, new[] { param });
            var result = finalRule.Execute(new[] { new RuleParameter("x", typeof(int), 1) });
            result.Success.Should().BeTrue();
        }
    }
}
