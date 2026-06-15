using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for action-only rules, custom namespaces, and mutation guards.
    /// </summary>
    public class RuleEdgeCaseTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;
        private readonly ExpressionCompiler _compiler;

        public RuleEdgeCaseTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice", IsAdult = false })
            };
            _namespaces = new[] { "RoslynRules.Tests", "System" };
            _compiler = TestCompiler.Instance;
        }

        [Fact]
        public void Execute_ActionOnlyNoExpression_ReturnsTrue()
        {
            var rule = new Rule
            {
                Description = "Action only",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeTrue();
            result.Value.Should().BeNull(); // Actions return null, not the assignment result
            var customer = (TestCustomer)_parameters[0].Value!;
            customer.IsAdult.Should().BeTrue();
        }

        [Fact]
        public void Validate_EmptyRule_ThrowsRuleValidationException()
        {
            var rule = new Rule
            {
                Description = "Empty rule",
                IsActive = true
            };

            var act = () => rule.Validate();
            act.Should().Throw<RuleValidationException>()
                .WithMessage("*no Expression, Action, or active ChildRules*");
        }

        [Fact]
        public void Execute_InactiveRule_LogsSkipped()
        {
            var logger = new TestLogger<Rule>();
            var rule = new Rule
            {
                Description = "Inactive",
                Expression = "customer.Age > 0",
                IsActive = false,
                Logger = logger
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);
            rule.Execute(_parameters);

            logger.EventIds.Should().ContainSingle();
            logger.EventIds[0].Id.Should().Be(1001); // RuleSkipped
        }

        [Fact]
        public void Compile_InvalidSyntax_ThrowsCompilationError()
        {
            var rule = new Rule
            {
                Description = "Bad syntax",
                Expression = "customer.Age >== 18", // Invalid operator
                IsActive = true
            };

            
            var act = () => rule.Compile(_compiler, _parameters, _namespaces);
            act.Should().Throw<Exception>()
                .WithInnerException<InvalidOperationException>()
                .WithMessage("*Compilation failed*");
        }

        [Fact]
        public void Compile_MultipleParameters_CompilesSuccessfully()
        {
            var parameters = new[]
            {
                new RuleParameter("a", typeof(int), 1),
                new RuleParameter("b", typeof(int), 2)
            };

            var rule = new Rule
            {
                Description = "Multi-param",
                Expression = "a > 0 && b > 0",
                IsActive = true
            };

            
            // Act - should compile and execute successfully
            rule.Compile(_compiler, parameters, _namespaces);
            
            // Assert - rule should be executable (no exception = compiled successfully)
            var result = rule.Execute(parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_MultipleParameters_DifferentTypes()
        {
            var parameters = new[]
            {
                new RuleParameter("name", typeof(string), "Alice"),
                new RuleParameter("age", typeof(int), 25)
            };

            var rule = new Rule
            {
                Description = "Multi-param types",
                Expression = "name.Length > 0 && age >= 18",
                IsActive = true
            };

            rule.Compile(_compiler, parameters, _namespaces);
            
            var result = rule.Execute(parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_MultipleParameters_FailsWhenExpressionFalse()
        {
            var parameters = new[]
            {
                new RuleParameter("x", typeof(int), 5),
                new RuleParameter("y", typeof(int), 3)
            };

            var rule = new Rule
            {
                Description = "Multi-param false",
                Expression = "x > y",
                IsActive = true
            };

            rule.Compile(_compiler, parameters, _namespaces);
            
            // Should fail when x <= y
            var failParams = new[]
            {
                new RuleParameter("x", typeof(int), 1),
                new RuleParameter("y", typeof(int), 10)
            };
            
            var result = rule.Execute(failParams);
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_ExpressionAndAction_BothRun()
        {
            var rule = new Rule
            {
                Description = "Check and set",
                Expression = "customer.Age >= 18",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeTrue();
            var customer = (TestCustomer)_parameters[0].Value!;
            customer.IsAdult.Should().BeTrue();
        }

        [Fact]
        public void Execute_ExpressionFalse_ActionNotRun()
        {
            var rule = new Rule
            {
                Description = "Check and set",
                Expression = "customer.Age >= 100",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeFalse();
            var customer = (TestCustomer)_parameters[0].Value!;
            customer.IsAdult.Should().BeFalse(); // Action not executed
        }

        [Fact]
        public void Compile_CustomNamespaces_ResolvesTypes()
        {
            var rule = new Rule
            {
                Description = "Use System namespace",
                Expression = "string.IsNullOrEmpty(customer.Name) == false",
                IsActive = true
            };

            var customNamespaces = new[] { "RoslynRules.Tests", "System" };
            
            rule.Compile(_compiler, _parameters, customNamespaces);

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Compile_MutationAfterCompile_Throws()
        {
            var rule = new Rule
            {
                Description = "Immutable after compile",
                Expression = "true",
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var act = () => rule.Expression = "false";
            act.Should().Throw<RuleCompilationException>()
                .WithMessage("*Cannot modify*after rule has been compiled*");
        }

        [Fact]
        public void Compile_MutationDescriptionAfterCompile_Throws()
        {
            var rule = new Rule
            {
                Description = "Immutable after compile",
                Expression = "true",
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var act = () => rule.Description = "New description";
            act.Should().Throw<RuleCompilationException>();
        }

        [Fact]
        public void Compile_MutationActionAfterCompile_Throws()
        {
            var rule = new Rule
            {
                Description = "Immutable after compile",
                Expression = "true",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var act = () => rule.Action = "customer.IsAdult = false";
            act.Should().Throw<RuleCompilationException>();
        }

        [Fact]
        public void Execute_RuntimeException_ReturnsFailureWithException()
        {
            var rule = new Rule
            {
                Description = "Divide by zero",
                Expression = "1 / (customer.Age - 25) == 1", // Age=25, so division by zero
                IsActive = true
            };

            
            rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeFalse();
            result.Exception.Should().NotBeNull();
            result.Exception.Should().BeOfType<DivideByZeroException>();
        }

        [Fact]
        public void Execute_RuntimeNullReference_ReturnsFailureWithException()
        {
            var customer = new TestCustomer { Age = 25, Name = null! };
            var parameters = new[] { new RuleParameter("customer", typeof(TestCustomer), customer) };

            var rule = new Rule
            {
                Description = "Null reference",
                Expression = "customer.Name.Length > 0", // Name is null
                IsActive = true
            };

            
            rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeFalse();
            result.Exception.Should().NotBeNull();
            result.Exception.Should().BeOfType<NullReferenceException>();
        }
    }
}


