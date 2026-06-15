using FluentAssertions;
using Microsoft.Extensions.Logging;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for async expressions, logging, and workflow edge cases.
    /// </summary>
    public class AsyncAndLoggingTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;
        private readonly ExpressionCompiler _compiler;

        public AsyncAndLoggingTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _namespaces = new[] { "RoslynRules.Tests", "System", "System.Threading.Tasks" };
            _compiler = TestCompiler.Instance;
        }

        [Fact]
        public async Task ExecuteAsync_AsyncExpression_ReturnsTrue()
        {
            var rule = new Rule
            {
                Description = "Async expression",
                Expression = "await Task.FromResult(customer.Age >= 18)",
                IsActive = true
            };

            rule.Compile(_compiler, _parameters, _namespaces);

            var result = await rule.ExecuteAsync(_parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_AsyncExpression_ReturnsFalse()
        {
            var rule = new Rule
            {
                Description = "Async expression false",
                Expression = "await Task.FromResult(customer.Age >= 100)",
                IsActive = true
            };

            rule.Compile(_compiler, _parameters, _namespaces);

            var result = await rule.ExecuteAsync(_parameters);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteAsync_AsyncAction_Executes()
        {
            var rule = new Rule
            {
                Description = "Async action",
                Expression = "true",
                Action = "await Task.Run(() => customer.IsAdult = true)",
                IsActive = true
            };

            rule.Compile(_compiler, _parameters, _namespaces);

            var result = await rule.ExecuteAsync(_parameters);

            result.Success.Should().BeTrue();
            var customer = (TestCustomer)_parameters[0].Value!;
            customer.IsAdult.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ChildAsync_ParentAwaits()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Async child",
                Expression = "await Task.FromResult(customer.Age > 0)",
                IsActive = true
            };

            parent.ChildRules.Add(child);

            parent.Compile(_compiler, _parameters, _namespaces);

            var result = await parent.ExecuteAsync(_parameters);

            result.Success.Should().BeTrue();
            result.ChildResults.Should().HaveCount(1);
            result.ChildResults[0].Success.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_RuntimeException_ReturnsFailureWithException()
        {
            var rule = new Rule
            {
                Description = "Async runtime error",
                Expression = "await Task.Run(() => 1 / (customer.Age - 25) == 1)",
                IsActive = true
            };

            rule.Compile(_compiler, _parameters, _namespaces);

            var result = await rule.ExecuteAsync(_parameters);

            result.Success.Should().BeFalse();
            result.Exception.Should().NotBeNull();
        }

        [Fact]
        public void Execute_WithLogger_FiresEvent()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Logged rule",
                Expression = "customer.Age >= 18",
                IsActive = true,
                Logger = logger
            };

            rule.Compile(_compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.LogMessages.Should().ContainSingle();
            logger.LogMessages[0].Should().Contain("Logged rule");
            logger.EventIds[0].Id.Should().Be(1002); // RulePassed
        }

        [Fact]
        public void Execute_FailingRule_LogsFailure()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Failing rule",
                Expression = "customer.Age >= 100",
                IsActive = true,
                Logger = logger
            };

            rule.Compile(_compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.LogMessages.Should().ContainSingle();
            logger.LogMessages[0].Should().Contain("Failing rule");
            logger.EventIds[0].Id.Should().Be(1003); // RuleFailed
        }

        [Fact]
        public void Execute_RuntimeError_LogsException()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Error rule",
                Expression = "1 / (customer.Age - 25) == 1",
                IsActive = true,
                Logger = logger
            };

            rule.Compile(_compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.LogMessages.Should().ContainSingle();
            logger.LogMessages[0].Should().Contain("Error rule");
            logger.EventIds[0].Id.Should().Be(1004); // RuleError
        }

        [Fact]
        public void Validate_DuplicateRuleIds_ThrowsDuplicateRuleIdException()
        {
            var workflow = new global::RoslynRules.Models.Workflow();
            var sharedId = Guid.NewGuid();
            var rule1 = new Rule(sharedId) { Description = "Rule 1", Expression = "true" };
            var rule2 = new Rule(sharedId) { Description = "Rule 2", Expression = "true" };
            
            workflow.Rules.Add(rule1);
            workflow.Rules.Add(rule2);

            var act = () => workflow.Validate();
            act.Should().Throw<DuplicateRuleIdException>();
        }

        [Fact]
        public void Validate_EmptyWorkflow_ThrowsWorkflowException()
        {
            var workflow = new Workflow { Description = "Empty" };

            var act = () => workflow.Validate();
            act.Should().Throw<WorkflowException>();
        }
    }

    /// <summary>
    /// Test logger that captures RuleExecutedEvent for verification.
    /// </summary>
    public class TestLogger<T> : ILogger<T>
    {
        public List<RuleExecutedEvent> Events { get; } = new List<RuleExecutedEvent>();
        public List<string> LogMessages { get; } = new List<string>();
        public List<EventId> EventIds { get; } = new List<EventId>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null!;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            LogMessages.Add(message);
            EventIds.Add(eventId);
            
            // Extract RuleExecutedEvent if present in the state
            if (state is RuleExecutedEvent evt)
                Events.Add(evt);
        }
    }
}
