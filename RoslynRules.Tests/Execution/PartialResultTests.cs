using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for partial results showing which child rule failed.
    /// </summary>
    public class PartialResultTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;

        private readonly ExpressionCompiler _compiler;

        public PartialResultTests()
        {
            _compiler = TestCompiler.Instance;
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _namespaces = new[] { "RoslynRules.Tests" };
        }

        [Fact]
        public void Execute_ChildFails_ResultIncludesChildRuleId()
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
            result.RuleId.Should().Be(parent.Id);
            result.RuleDescription.Should().Be("Parent");
            
            // Should have child results
            result.ChildResults.Should().HaveCount(1);
            result.ChildResults[0].RuleId.Should().Be(child.Id);
            result.ChildResults[0].RuleDescription.Should().Be("Child fails");
            result.ChildResults[0].Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_AllChildrenPass_ResultHasEmptyChildResults()
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
            result.ChildResults.Should().HaveCount(1);
            result.ChildResults[0].Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_FirstFailure_ReturnsCorrectChild()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child1 = new Rule
            {
                Description = "First child passes",
                Expression = "customer.Age > 0",
                IsActive = true
            };

            var child2 = new Rule
            {
                Description = "Second child fails",
                Expression = "customer.Age > 100",
                IsActive = true
            };

            parent.ChildRules.Add(child1);
            parent.ChildRules.Add(child2);

                        parent.Compile(_compiler, _parameters, _namespaces);

            var result = parent.Execute(_parameters);

            result.Success.Should().BeFalse();
            
            // FirstFailure helper should point to child2
            result.FirstFailure.Should().NotBeNull();
            result.FirstFailure.Value.RuleDescription.Should().Be("Second child fails");
            result.FirstFailure.Value.Success.Should().BeFalse();
            
            // AllFailures should contain only the failing child
            result.AllFailures.Should().HaveCount(1);
        }

        [Fact]
        public void Execute_InactiveRule_ResultShowsInactive()
        {
            var rule = new Rule
            {
                Description = "Inactive rule",
                Expression = "customer.Age > 100",
                IsActive = false
            };

                        rule.Compile(_compiler, _parameters, _namespaces);

            var result = rule.Execute(_parameters);

            result.Success.Should().BeTrue(); // Inactive = skipped = success
            result.IsActive.Should().BeFalse();
            result.RuleDescription.Should().Be("Inactive rule");
        }
    }
}




