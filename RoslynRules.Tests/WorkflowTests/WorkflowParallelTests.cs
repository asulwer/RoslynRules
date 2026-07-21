using FluentAssertions;
using RoslynRules.Models;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.WorkflowTests
{
    /// <summary>
    /// Tests for Workflow.ExecuteParallel and ExecuteParallelAsync.
    /// </summary>
    public class WorkflowParallelTests
    {
        private readonly RuleParameter[] _parameters;

        public WorkflowParallelTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        [Fact]
        public void ExecuteParallel_MultipleRules_ReturnsAll()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule 1",
                        Expression = "customer.Age > 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Rule 2",
                        Expression = "customer.Name != null",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void ExecuteParallel_DanglingDependency_ToleratedLikeSequentialExecute()
        {
            // A rule depends on an id that is not an active rule in this workflow.
            var dependent = new Rule
            {
                Description = "Depends on a missing rule",
                Expression = "customer.Age > 0",
                IsActive = true,
                DependsOnRuleId = System.Guid.NewGuid() // points at nothing in this workflow
            };

            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Independent", Expression = "customer.Name != null", IsActive = true },
                    dependent
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            // Sequential Execute tolerates the dangling dependency (runs it as independent).
            var sequential = workflow.Execute(_parameters).ToList();

            // Parallel must behave identically — not throw a "dependency resolution" error.
            var parallel = workflow.Invoking(w => w.ExecuteParallel(_parameters)).Should().NotThrow().Which;

            sequential.Should().HaveCount(2);
            sequential.All(r => r.Success).Should().BeTrue();
            parallel.Should().HaveCount(2);
            parallel.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void ExecuteParallel_InactiveWorkflow_ReturnsEmpty()
        {
            var workflow = new Workflow
            {
                IsActive = false,
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule",
                        Expression = "true",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().BeEmpty();
        }

        [Fact]
        public void ExecuteParallel_NoActiveRules_ReturnsEmpty()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Inactive",
                        Expression = "true",
                        IsActive = false
                    }
                }
            };

            var results = workflow.ExecuteParallel(_parameters);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteParallelAsync_MultipleRules_ReturnsAll()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule 1",
                        Expression = "customer.Age > 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Rule 2",
                        Expression = "customer.Name != null",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = await workflow.ExecuteParallelAsync(_parameters, TestContext.Current.CancellationToken);

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteParallelAsync_InactiveWorkflow_ReturnsEmpty()
        {
            var workflow = new Workflow
            {
                IsActive = false,
                Rules =
                {
                    new Rule
                    {
                        Description = "Rule",
                        Expression = "true",
                        IsActive = true
                    }
                }
            };

            workflow.Compile(_parameters, new[] { "RoslynRules.Tests" });

            var results = await workflow.ExecuteParallelAsync(_parameters, TestContext.Current.CancellationToken);

            results.Should().BeEmpty();
        }
    }
}
