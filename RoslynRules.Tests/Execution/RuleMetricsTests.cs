using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for RuleMetrics — per-rule execution statistics.
    /// </summary>
    public class RuleMetricsTests
    {
        [Fact]
        public void Metrics_InitialState_AllZero()
        {
            var rule = new Rule
            {
                Description = "Test",
                Expression = "x > 0",
                IsActive = true
            };

            rule.Metrics.EvalCount.Should().Be(0);
            rule.Metrics.FailureCount.Should().Be(0);
            rule.Metrics.AverageExecutionTimeMs.Should().Be(0);
            rule.Metrics.FailureRatePercent.Should().Be(0);
            rule.Metrics.LastExecuted.Should().BeNull();
        }

        [Fact]
        public void Execute_SuccessfulRule_IncrementsEvalCount()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            rule.Execute(parameters);

            rule.Metrics.EvalCount.Should().Be(1);
            rule.Metrics.FailureCount.Should().Be(0);
            rule.Metrics.FailureRatePercent.Should().Be(0);
        }

        [Fact]
        public void Execute_FailingRule_IncrementsFailureCount()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), -1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            rule.Execute(parameters);

            rule.Metrics.EvalCount.Should().Be(1);
            rule.Metrics.FailureCount.Should().Be(1);
            rule.Metrics.FailureRatePercent.Should().Be(100);
        }

        [Fact]
        public void Execute_MultipleTimes_Accumulates()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            rule.Execute(parameters); // success
            rule.Execute(parameters); // success
            rule.Execute(parameters); // success

            rule.Metrics.EvalCount.Should().Be(3);
            rule.Metrics.FailureCount.Should().Be(0);
        }

        [Fact]
        public void Execute_MixedResults_CorrectFailureRate()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var goodParams = new[] { new RuleParameter("x", typeof(int), 1) };
            var badParams = new[] { new RuleParameter("x", typeof(int), -1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), goodParams);

            rule.Execute(goodParams); // success
            rule.Execute(goodParams); // success
            rule.Execute(badParams);  // failure

            rule.Metrics.EvalCount.Should().Be(3);
            rule.Metrics.FailureCount.Should().Be(1);
            rule.Metrics.FailureRatePercent.Should().BeApproximately(33.33, 0.01);
        }

        [Fact]
        public void Execute_SetsLastExecuted()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            var before = DateTime.UtcNow.AddSeconds(-1);
            rule.Execute(parameters);
            var after = DateTime.UtcNow.AddSeconds(1);

            rule.Metrics.LastExecuted.Should().NotBeNull();
            rule.Metrics.LastExecuted.Should().BeAfter(before);
            rule.Metrics.LastExecuted.Should().BeBefore(after);
        }

        [Fact]
        public void Execute_RecordsExecutionTime()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            rule.Execute(parameters);

            rule.Metrics.AverageExecutionTimeMs.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ClearCache_ResetsMetrics()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            rule.Execute(parameters);
            rule.ClearCache();

            rule.Metrics.EvalCount.Should().Be(0);
            rule.Metrics.FailureCount.Should().Be(0);
            rule.Metrics.LastExecuted.Should().BeNull();
        }

        [Fact]
        public async Task ExecuteAsync_SuccessfulRule_IncrementsEvalCount()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            await rule.ExecuteAsync(parameters);

            rule.Metrics.EvalCount.Should().Be(1);
        }

        [Fact]
        public async Task ExecuteAsync_FailingRule_IncrementsFailureCount()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), -1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            await rule.ExecuteAsync(parameters);

            rule.Metrics.FailureCount.Should().Be(1);
        }

        [Fact]
        public void Metrics_ThreadSafety_ConcurrentExecutions()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };
            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            Parallel.For(0, 100, _ => rule.Execute(parameters));

            rule.Metrics.EvalCount.Should().Be(100);
            rule.Metrics.FailureCount.Should().Be(0);
        }
    }
}
