using FluentAssertions;
using RoslynRules.Exceptions;
using System;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Core
{
    /// <summary>
    /// Tests for all exception constructors to maximize coverage.
    /// </summary>
    public class ExceptionTests
    {
        [Fact]
        public void RuleTimeoutException_Constructor()
        {
            var id = Guid.NewGuid();
            var timeout = TimeSpan.FromSeconds(5);
            var ex = new RuleTimeoutException(id, timeout);
            ex.RuleId.Should().Be(id);
            ex.Timeout.Should().Be(timeout);
            ex.Message.Should().Contain(id.ToString());
            ex.Message.Should().Contain("5");
        }

        [Fact]
        public void RuleTimeoutException_InheritsFromRulesException()
        {
            var ex = new RuleTimeoutException(Guid.NewGuid(), TimeSpan.FromSeconds(1));
            Assert.IsAssignableFrom<RulesException>(ex);
        }

        [Fact]
        public void RuleCompilationException_Constructor()
        {
            var ex1 = new RuleCompilationException("message");
            ex1.Message.Should().Be("message");

            var inner = new InvalidOperationException("inner");
            var ex2 = new RuleCompilationException("message", inner);
            ex2.Message.Should().Be("message");
            ex2.InnerException.Should().Be(inner);
        }

        [Fact]
        public void CircularReferenceException_Properties()
        {
            var id = Guid.NewGuid();
            var ex = new CircularReferenceException(id, "Test rule");
            ex.RuleId.Should().Be(id);
            ex.RuleDescription.Should().Be("Test rule");
            ex.Message.Should().Contain(id.ToString());
        }

        [Fact]
        public void SyntaxErrorException_Properties()
        {
            var ex = new SyntaxErrorException("bad expression", new[] { "error1", "error2" });
            ex.Expression.Should().Be("bad expression");
            ex.Errors.Should().Equal("error1", "error2");
            ex.Message.Should().Contain("error1");
        }

        [Fact]
        public void NotCompiledException_Properties()
        {
            var id = Guid.NewGuid();
            var ex = new NotCompiledException(id);
            ex.RuleId.Should().Be(id);
            ex.Message.Should().Contain(id.ToString());
        }

        [Fact]
        public void DuplicateRuleIdException_Properties()
        {
            var id = Guid.NewGuid();
            var ex = new DuplicateRuleIdException(new[] { id });
            ex.DuplicateIds.Should().Contain(id);
            ex.Message.Should().Contain(id.ToString());
        }

        [Fact]
        public void WorkflowException_Constructor()
        {
            var ex = new WorkflowException("workflow error");
            ex.Message.Should().Be("workflow error");
        }

        [Fact]
        public void RuleValidationException_Constructor()
        {
            var ex = new RuleValidationException("validation error");
            ex.Message.Should().Be("validation error");
        }
    }
}