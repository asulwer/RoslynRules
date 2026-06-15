using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.Core
{
    /// <summary>
    /// Tests for Rule.ValidateSemantics — semantic validation that catches
    /// undefined variables, missing types, and other compile-time errors.
    /// </summary>
    public class SemanticValidationTests
    {
        private readonly RuleParameter[] _parameters;

        public SemanticValidationTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25 })
            };
        }

        [Fact]
        public void ValidateSemantics_ValidExpression_Passes()
        {
            var rule = new Rule
            {
                Description = "Valid",
                Expression = "customer.Age >= 18"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateSemantics_ValidAction_Passes()
        {
            var rule = new Rule
            {
                Description = "Valid action",
                Expression = "true",
                Action = "customer.IsAdult = true"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateSemantics_UndefinedVariable_ThrowsRuleCompilationException()
        {
            var rule = new Rule
            {
                Description = "Bad expression",
                Expression = "nonExistentVariable > 5"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().Throw<RuleCompilationException>()
                .WithMessage("*Semantic error*");
        }

        [Fact]
        public void ValidateSemantics_MissingProperty_ThrowsRuleCompilationException()
        {
            var rule = new Rule
            {
                Description = "Bad property",
                Expression = "customer.NonExistentProperty > 5"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().Throw<RuleCompilationException>()
                .WithMessage("*Semantic error*");
        }

        [Fact]
        public void ValidateSemantics_WrongMethodSignature_ThrowsRuleCompilationException()
        {
            var rule = new Rule
            {
                Description = "Bad method call",
                Expression = "customer.Name.StartsWith(123)"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().Throw<RuleCompilationException>()
                .WithMessage("*Semantic error*");
        }

        [Fact]
        public void ValidateSemantics_BadAction_ThrowsRuleCompilationException()
        {
            var rule = new Rule
            {
                Description = "Bad action",
                Expression = "true",
                Action = "customer.NonExistentProperty = true"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().Throw<RuleCompilationException>()
                .WithMessage("*Semantic error*")
                .WithMessage("*Action*");
        }

        [Fact]
        public void ValidateSemantics_EmptyRule_Passes()
        {
            var rule = new Rule
            {
                Description = "Empty",
                Expression = "true"
            };

            var act = () => rule.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_SyntaxOnly_UndefinedVariable_Passes()
        {
            // Validate() (syntax-only) should pass for undefined variables
            var rule = new Rule
            {
                Description = "Syntax valid, semantics bad",
                Expression = "nonExistentVariable > 5"
            };

            var act = () => rule.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateSemantics_ChildRules_ValidatedRecursively()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true"
            };

            var child = new Rule
            {
                Description = "Child with bad expression",
                Expression = "nonExistentVariable > 5"
            };

            parent.ChildRules.Add(child);

            var act = () => parent.ValidateSemantics(TestCompiler.Instance, _parameters);
            act.Should().Throw<RuleCompilationException>()
                .WithMessage("*Semantic error*")
                .WithMessage("*Child*");
        }
    }
}
