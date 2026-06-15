using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Threading.Tasks;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for per-rule timeout functionality.
    /// </summary>
    public class RuleTimeoutTests
    {
        private readonly RuleParameter[] _compileParams = new[]
        {
            new RuleParameter("x", typeof(int))
        };

        private readonly RuleParameter[] _executeParams = new[]
        {
            new RuleParameter("x", typeof(int), 1)
        };

        [Fact]
        public void Rule_WithoutTimeout_Completes_Successfully()
        {
            var rule = new Rule
            {
                Expression = "x == 1",
                Description = "Fast rule"
                // No timeout set
            };

            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), _compileParams);
            var result = rule.Execute(_executeParams);

            Assert.True(result.Success);
        }

        [Fact]
        public void Rule_WithTimeout_FastRule_Completes_Successfully()
        {
            var rule = new Rule
            {
                Expression = "x == 1",
                Description = "Fast rule with timeout",
                Timeout = TimeSpan.FromSeconds(5)
            };

            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), _compileParams);
            var result = rule.Execute(_executeParams);

            Assert.True(result.Success);
        }

        [Fact]
        public void Rule_Timeout_IsImmutable_After_Compile()
        {
            var rule = new Rule
            {
                Expression = "true",
                Timeout = TimeSpan.FromSeconds(1)
            };

            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), _compileParams);

            Assert.Throws<RuleCompilationException>(() => rule.Timeout = TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Rule_Timeout_CanBeNull()
        {
            var rule = new Rule
            {
                Expression = "true"
                // Timeout is null by default
            };

            Assert.Null(rule.Timeout);
        }

        [Fact]
        public void Rule_Timeout_Property_SetAndGet()
        {
            var rule = new Rule();
            var timeout = TimeSpan.FromSeconds(3.5);

            rule.Timeout = timeout;

            Assert.Equal(timeout, rule.Timeout);
        }

        [Fact]
        public void RuleTimeoutException_HasCorrectProperties()
        {
            var ruleId = Guid.NewGuid();
            var timeout = TimeSpan.FromSeconds(2);

            var ex = new RuleTimeoutException(ruleId, timeout);

            Assert.Equal(ruleId, ex.RuleId);
            Assert.Equal(timeout, ex.Timeout);
            Assert.Contains("2s", ex.Message);
        }

        [Fact]
        public void RuleTimeoutException_InheritsFromRulesException()
        {
            var ex = new RuleTimeoutException(Guid.NewGuid(), TimeSpan.FromSeconds(1));

            Assert.IsAssignableFrom<RulesException>(ex);
        }

        [Fact]
        public async Task Rule_WithTimeout_Async_Completes_WhenFast()
        {
            var rule = new Rule
            {
                Expression = "x == 1",
                Description = "Fast async rule with timeout",
                Timeout = TimeSpan.FromSeconds(5)
            };

            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), _compileParams);
            var result = await rule.ExecuteAsync(_executeParams);

            Assert.True(result.Success);
        }
    }
}