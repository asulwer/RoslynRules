using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.Core
{
    /// <summary>
    /// Tests for Rule mutation guards and edge cases.
    /// </summary>
    public class RuleMutationTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;

        private readonly ExpressionCompiler _compiler;

        public RuleMutationTests()
        {
            _compiler = TestCompiler.Instance;
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25 })
            };
            _namespaces = new[] { "RoslynRules.Tests" };
        }

        [Fact]
        public void Compile_MutateIsActive_Throws()
        {
            var rule = new Rule
            {
                Description = "Test",
                Expression = "true",
                IsActive = true
            };

                        rule.Compile(_compiler, _parameters, _namespaces);

            var act = () => rule.IsActive = false;
            act.Should().Throw<RuleCompilationException>();
        }

        [Fact]
        public void Execute_ExpressionFalseWithAction_ActionNotRun()
        {
            var rule = new Rule
            {
                Description = "Conditional action",
                Expression = "customer.Age > 100",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

                        rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeFalse();
            ((TestCustomer)_parameters[0].Value!).IsAdult.Should().BeFalse();
        }

        [Fact]
        public void Validate_CircularReferenceDeep_Throws()
        {
            var root = new Rule { Description = "Root", Expression = "true", IsActive = true };
            var a = new Rule { Description = "A", Expression = "true", IsActive = true };
            var b = new Rule { Description = "B", Expression = "true", IsActive = true };
            var c = new Rule { Description = "C", Expression = "true", IsActive = true };

            root.ChildRules.Add(a);
            a.ChildRules.Add(b);
            b.ChildRules.Add(c);
            c.ChildRules.Add(a); // Cycle: a -> b -> c -> a

            var act = () => root.Validate();
            act.Should().Throw<CircularReferenceException>();
        }

        [Fact]
        public void Execute_ChildFails_ParentFails()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Failing child",
                Expression = "customer.Age > 100",
                IsActive = true
            };

            parent.ChildRules.Add(child);

                        parent.Compile(_compiler, _parameters, _namespaces);

            var result = parent.Execute(_parameters);

            result.Success.Should().BeFalse();
            result.ChildResults.Should().HaveCount(1);
            result.ChildResults[0].Success.Should().BeFalse();
        }
    }
}




