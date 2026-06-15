using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Models;
using Xunit;

namespace RoslynRules.Tests.WorkflowTests
{
    public class WorkflowTests
    {
        private readonly RuleParameter[] _parameters;
        private readonly string[] _namespaces;

        public WorkflowTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _namespaces = new[] { "RoslynRules.Tests" };
        }

        [Fact]
        public void Validate_EmptyWorkflow_ThrowsWorkflowException()
        {
            var workflow = new Workflow
            {
                Description = "Empty",
                Rules = new List<Rule>()
            };

            var act = () => workflow.Validate();
            act.Should().Throw<RoslynRules.Exceptions.WorkflowException>()
                .WithMessage("*has no active rules*");
        }

        [Fact]
        public void Validate_WorkflowWithActiveRules_DoesNotThrow()
        {
            var workflow = new Workflow
            {
                Description = "Valid",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule1", Expression = "customer.Age > 0", IsActive = true }
                }
            };

            var act = () => workflow.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_DuplicateRuleIds_ThrowsDuplicateRuleIdException()
        {
            var sharedId = Guid.NewGuid();
            var rule1 = new Rule(sharedId) { Description = "Rule1", Expression = "true", IsActive = true };
            var rule2 = new Rule(sharedId) { Description = "Rule2", Expression = "true", IsActive = true };

            var workflow = new Workflow
            {
                Description = "Dup IDs",
                Rules = new List<Rule> { rule1, rule2 }
            };

            var act = () => workflow.Validate();
            act.Should().Throw<RoslynRules.Exceptions.DuplicateRuleIdException>()
                .WithMessage("*duplicate rule IDs*");
        }

        [Fact]
        public void Execute_MultipleRules_ReturnsAllResults()
        {
            var workflow = new Workflow
            {
                Description = "Multi",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule1", Expression = "customer.Age >= 18", IsActive = true },
                    new Rule { Description = "Rule2", Expression = "customer.Name.StartsWith(\"A\")", IsActive = true },
                    new Rule { Description = "Rule3", Expression = "customer.Age > 100", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters, _namespaces);

            var results = workflow.Execute(_parameters).ToList();

            results.Should().HaveCount(3);
            results[0].Success.Should().BeTrue();
            results[1].Success.Should().BeTrue();
            results[2].Success.Should().BeFalse();
        }

        [Fact]
        public void ExecuteParallel_MultipleRules_ReturnsAllResults()
        {
            var workflow = new Workflow
            {
                Description = "Parallel",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule1", Expression = "customer.Age >= 18", IsActive = true },
                    new Rule { Description = "Rule2", Expression = "customer.Age >= 0", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters, _namespaces);

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void Execute_InactiveWorkflow_ReturnsEmptyResults()
        {
            var workflow = new Workflow
            {
                Description = "Inactive",
                IsActive = false,
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule1", Expression = "true", IsActive = true }
                }
            };

            var results = workflow.Execute(_parameters).ToList();

            results.Should().BeEmpty();
        }
    }
}
