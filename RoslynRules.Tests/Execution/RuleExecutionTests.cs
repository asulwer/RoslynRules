using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    public class RuleExecutionTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;

        private readonly ExpressionCompiler _compiler;

        public RuleExecutionTests()
        {
            _compiler = TestCompiler.Instance;
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _namespaces = new[] { "RoslynRules.Tests" };
        }

        [Fact]
        public void Execute_SimpleExpression_ReturnsTrue()
        {
            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

                        rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_SimpleExpression_ReturnsFalse()
        {
            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 30",
                IsActive = true
            };

                        rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_InactiveRule_ReturnsTrue()
        {
            var rule = new Rule
            {
                Description = "Inactive",
                Expression = "customer.Age >= 100",
                IsActive = false
            };

                        rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            // Inactive rules return true (skip)
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_Action_ModifiesParameter()
        {
            var customer = new TestCustomer { Age = 25, Name = "Alice" };
            var parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), customer)
            };

            var rule = new Rule
            {
                Description = "Mark adult",
                Expression = "customer.Age >= 18",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
            customer.IsAdult.Should().BeTrue();
        }

        [Fact]
        public void Execute_ParentWithChild_ChildFails_ParentFails()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child fails",
                Expression = "customer.Age > 100",
                IsActive = true
            };

            parent.ChildRules.Add(child);

                        parent.Compile(_compiler, _parameters, _namespaces);

            var result = parent.Execute(_parameters);

            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_ParentWithChild_ChildPasses_ParentPasses()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child passes",
                Expression = "customer.Age > 0",
                IsActive = true
            };

            parent.ChildRules.Add(child);

                        parent.Compile(_compiler, _parameters, _namespaces);

            var result = parent.Execute(_parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Compile_MultipleParameters_CompilesSuccessfully()
        {
            var rule = new Rule
            {
                Description = "Multi-param",
                Expression = "a > 0 && b > 0",
                IsActive = true
            };

            var parameters = new[]
            {
                new RuleParameter("a", typeof(int), 1),
                new RuleParameter("b", typeof(int), 2)
            };

            // Act - should not throw
            rule.Compile(_compiler, parameters, _namespaces);
            
            // Assert - rule should be executable (no exception = compiled successfully)
            var result = rule.Execute(parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_NotCompiled_ThrowsNotCompiledException()
        {
            var rule = new Rule
            {
                Description = "Not compiled",
                Expression = "customer.Age > 18",
                IsActive = true
            };

            var act = () => rule.Execute(_parameters);
            act.Should().Throw<RoslynRules.Exceptions.NotCompiledException>()
                .WithMessage("*must be compiled before execution*");
        }
        [Fact]
        public void Execute_ParameterNameMismatch_ThrowsInvalidOperationException()
        {
            var rule = new Rule
            {
                Description = "Name mismatch test",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compileParams = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            var executeParams = new[]
            {
                new RuleParameter("cust", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };

                        rule.Compile(_compiler, compileParams, _namespaces);

            var act = () => rule.Execute(executeParams);
            act.Should().Throw<RoslynRules.Exceptions.RuleValidationException>()
                .WithMessage("*Expected parameter name 'customer'*")
                .WithMessage("*but received 'cust'*");
        }

        [Fact]
        public void Execute_ParameterTypeMismatch_ThrowsArgumentException()
        {
            var rule = new Rule
            {
                Description = "Type mismatch test",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var compileParams = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            var executeParams = new[]
            {
                new RuleParameter("customer", typeof(string), "not a customer")
            };

                        rule.Compile(_compiler, compileParams, _namespaces);

            var act = () => rule.Execute(executeParams);
            act.Should().Throw<RoslynRules.Exceptions.RuleValidationException>()
                .WithMessage("*Expected type 'TestCustomer'*")
                .WithMessage("*but received 'String'*");
        }

        [Fact]
        public void Execute_ParameterNameAndTypeMatch_Succeeds()
        {
            var rule = new Rule
            {
                Description = "Matching parameters",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
        }
    }
}
