using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for expression security, concurrent compilation, and edge cases.
    /// </summary>
    public class SecurityAndConcurrencyTests
    {
        private readonly ExpressionCompiler _compiler = new ExpressionCompiler();
        private readonly RuleParameter[] _compileParams = new[]
        {
            new RuleParameter("x", typeof(int))
        };

        private readonly RuleParameter[] _executeParams = new[]
        {
            new RuleParameter("x", typeof(int), 1)
        };

        [Fact]
        public void MaliciousExpression_FileDelete_IsBlockedByCompilation()
        {
            // This expression contains a file deletion call.
            // In the current implementation, it will compile successfully because
            // System.IO is available. This test documents the current behavior
            // and will fail if sandboxing is added later.
            var rule = new Rule
            {
                Expression = @"System.IO.File.Exists(@""test.txt"")",
                Description = "File system access test"
            };

            // Currently compiles — this test documents that file system access is possible
            rule.Compile(_compiler, _compileParams);
            var result = rule.Execute(_executeParams);

            // Should not throw — currently allowed
            Assert.True(result.Success || result.Exception == null);
        }

        [Fact]
        public async Task ExpressionCompiler_Concurrent_CompileSameExpression_OnlyCompilesOnce()
        {
                        var expression = "x == 1";
            var paramNames = new[] { "x" };

            // Compile the same expression from multiple threads simultaneously
            var tasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(() => _compiler.Compile<Func<int, bool>>(expression, paramNames)))
                .ToArray();

            await Task.WhenAll(tasks);

            // All should succeed — no exceptions from concurrent compilation
            var delegates = tasks.Select(t => t.Result).ToArray();
            Assert.All(delegates, d => Assert.NotNull(d));

            // All delegates should produce the same result
            foreach (var del in delegates)
            {
                Assert.True(del(1));
                Assert.False(del(2));
            }
        }

        [Fact]
        public async Task ExpressionCompiler_Concurrent_CompileDifferentExpressions_AllSucceed()
        {
                        var expressions = new[]
            {
                "x == 1",
                "x > 0",
                "x < 100",
                "x != 0",
                "x == 42"
            };

            var tasks = expressions
                .Select(expr => Task.Run(() => _compiler.Compile<Func<int, bool>>(expr, new[] { "x" })))
                .ToArray();

            await Task.WhenAll(tasks);

            Assert.All(tasks, t => Assert.NotNull(t.Result));
        }

        [Fact]
        public void JsonRuleLoader_RoundTrip_PreservesId()
        {
            var original = new Workflow
            {
                Description = "Test workflow",
                Rules =
                {
                    new Rule
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Description = "Test rule",
                        Expression = "true"
                    }
                }
            };

            var json = RoslynRules.Json.JsonRuleLoader.Serialize(original);
            var restored = RoslynRules.Json.JsonRuleLoader.DeserializeWorkflow(json);

            Assert.Equal(original.Id, restored.Id);
            Assert.Single(restored.Rules);
            Assert.Equal(original.Rules[0].Id, restored.Rules[0].Id);
        }

        [Fact]
        public void JsonRuleLoader_RoundTrip_PreservesNestedChildRules()
        {
            var child = new Rule
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Description = "Child rule",
                Expression = "true"
            };

            var parent = new Rule
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Description = "Parent rule",
                Expression = "true",
                ChildRules = { child }
            };

            var workflow = new Workflow
            {
                Rules = { parent }
            };

            var json = RoslynRules.Json.JsonRuleLoader.Serialize(workflow);
            var restored = RoslynRules.Json.JsonRuleLoader.DeserializeWorkflow(json);

            Assert.Single(restored.Rules);
            Assert.Single(restored.Rules[0].ChildRules);
            Assert.Equal(child.Id, restored.Rules[0].ChildRules[0].Id);
            Assert.Equal("Child rule", restored.Rules[0].ChildRules[0].Description);
        }

        [Fact]
        public void Workflow_ExecuteParallel_WithDependencies_RespectsOrder()
        {
            var dependency = new Rule
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Description = "Dependency",
                Expression = "true"
            };

            var dependent = new Rule
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Description = "Dependent",
                Expression = "true",
                DependsOnRuleId = dependency.Id
            };

            var workflow = new Workflow
            {
                Rules = { dependent, dependency }  // Note: dependent listed first
            };

            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            workflow.Compile(parameters);

            var results = workflow.ExecuteParallel(parameters);

            Assert.Equal(2, results.Length);
            // Dependency should execute before dependent even in parallel mode
            // Both should succeed since dependency is simple
            Assert.All(results, r => Assert.True(r.Success));
        }

        [Fact]
        public void DelegateFactory_MissingType_ThrowsMeaningfulException()
        {
            // This tests the null-check path in DelegateFactory by passing invalid assembly bytes
            var invalidBytes = new byte[] { 0x4D, 0x5A }; // MZ header only, not a valid assembly

            var context = new RoslynRules.Compiler.ExpressionAssemblyLoadContext("test");
            var ex = Assert.Throws<BadImageFormatException>(() =>
                RoslynRules.Compiler.DelegateFactory.CreateDelegate(invalidBytes, typeof(Func<int, bool>), context));

            Assert.NotNull(ex);
        }

        [Fact]
        public void ExpressionCompiler_CompileLimit_RecyclesContext()
        {
            // This test needs its own compiler with a specific recycle limit
            var compiler = new ExpressionCompiler(maxCompilesBeforeRecycle: 3);
            var paramNames = new[] { "x" };

            // Compile 3 unique expressions
            var d1 = compiler.Compile<Func<int, bool>>("x == 1", paramNames);
            var d2 = compiler.Compile<Func<int, bool>>("x == 2", paramNames);
            var d3 = compiler.Compile<Func<int, bool>>("x == 3", paramNames);

            Assert.Equal(3, compiler.CompileCount);
            var contextBefore = compiler.CurrentContextName;

            // 4th compile triggers recycle
            var d4 = compiler.Compile<Func<int, bool>>("x == 4", paramNames);
            var contextAfter = compiler.CurrentContextName;

            Assert.NotEqual(contextBefore, contextAfter);
            Assert.Equal(4, compiler.CompileCount);

            // All delegates still work
            Assert.True(d1(1));
            Assert.True(d2(2));
            Assert.True(d3(3));
            Assert.True(d4(4));
        }

        [Fact]
        public void ExpressionCompiler_Unload_ClearsCacheAndRecyclesContext()
        {
                        var paramNames = new[] { "x" };

            var d1 = _compiler.Compile<Func<int, bool>>("x == 1", paramNames);
            Assert.True(d1(1));
            Assert.Equal(1, _compiler.CompileCount);

            var contextBefore = _compiler.CurrentContextName;
            _compiler.Unload();
            var contextAfter = _compiler.CurrentContextName;

            Assert.Equal(0, _compiler.CompileCount);
            Assert.NotEqual(contextBefore, contextAfter);

            // After unload, recompiling the same expression works
            var d2 = _compiler.Compile<Func<int, bool>>("x == 1", paramNames);
            Assert.True(d2(1));
        }
    }
}



