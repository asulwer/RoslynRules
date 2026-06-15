using FluentAssertions;
using RoslynRules.Compiler;
using Xunit;

namespace RoslynRules.Tests.Compiler
{
    /// <summary>
    /// Tests for IsAsyncExpression detection via Roslyn syntax tree.
    /// </summary>
    public class AsyncDetectionTests
    {
        [Fact]
        public void IsAsyncExpression_SimpleAwait_ReturnsTrue()
        {
            CodeGenerator.Generate("await Task.Delay(1)", typeof(bool), new[] { "x" }, new[] { typeof(object) })
                .Should().Contain("async Task");
        }

        [Fact]
        public void IsAsyncExpression_VariableNamedAwaiting_ReturnsFalse()
        {
            CodeGenerator.Generate("awaiting == true", typeof(bool), new[] { "x" }, new[] { typeof(object) })
                .Should().NotContain("async");
        }

        [Fact]
        public void IsAsyncExpression_Awaitable_ReturnsFalse()
        {
            CodeGenerator.Generate("awaitable.IsCompleted", typeof(bool), new[] { "x" }, new[] { typeof(object) })
                .Should().NotContain("async");
        }

        [Fact]
        public void IsAsyncExpression_AwaitInStringLiteral_ReturnsFalse()
        {
            CodeGenerator.Generate("x == \"await something\"", typeof(bool), new[] { "x" }, new[] { typeof(string) })
                .Should().NotContain("async");
        }

        [Fact]
        public void IsAsyncExpression_AwaitInComment_ReturnsFalse()
        {
            CodeGenerator.Generate("true // await Task.Delay(1)", typeof(bool), new[] { "x" }, new[] { typeof(object) })
                .Should().NotContain("async");
        }

        [Fact]
        public void IsAsyncExpression_MultilineAwait_ReturnsTrue()
        {
            var expr = @"
var t = Task.Delay(1);
await t;
return true;";
            CodeGenerator.Generate(expr, typeof(bool), new[] { "x" }, new[] { typeof(object) })
                .Should().Contain("async Task");
        }
    }
}
