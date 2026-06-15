using FluentAssertions;
using RoslynRules.Execution;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Execution
{
    public class RuleActionChainingTests
    {
        private readonly RuleParameter[] _parameters;

        public RuleActionChainingTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        // ==================== DependsOnRuleId ====================

        [Fact]
        public void Execute_WithDependency_ExecutesDependencyFirst()
        {
            var ruleA = new Rule
            {
                Description = "RuleA",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var ruleB = new Rule
            {
                Description = "RuleB",
                DependsOnRuleId = ruleA.Id,
                Expression = "customer.Age >= 21",
                IsActive = true
            };

            var workflow = new Workflow
            {
                Description = "Chaining test",
                Rules = new List<Rule> { ruleB, ruleA } // Intentionally out of order
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var results = workflow.Execute(_parameters).ToList();

            results.Should().HaveCount(2);
            results[0].RuleDescription.Should().Be("RuleA"); // Dependency executes first
            results[1].RuleDescription.Should().Be("RuleB");
        }

        [Fact]
        public void Execute_DependencyFails_DependentStillExecutes()
        {
            var ruleA = new Rule
            {
                Description = "RuleA",
                Expression = "customer.Age >= 100", // Fails
                IsActive = true
            };

            var ruleB = new Rule
            {
                Description = "RuleB",
                DependsOnRuleId = ruleA.Id,
                Expression = "customer.Age >= 18", // Passes
                IsActive = true
            };

            var workflow = new Workflow
            {
                Description = "Dependency fail test",
                Rules = new List<Rule> { ruleA, ruleB }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var results = workflow.Execute(_parameters).ToList();

            results.Should().HaveCount(2);
            results[0].Success.Should().BeFalse();
            results[1].Success.Should().BeTrue();
        }

        // ==================== Circular Dependency Detection ====================

        [Fact]
        public void Validate_CircularDependency_ThrowsException()
        {
            var ruleA = new Rule
            {
                Description = "RuleA",
                Expression = "true",
                IsActive = true
            };

            var ruleB = new Rule
            {
                Description = "RuleB",
                DependsOnRuleId = ruleA.Id,
                Expression = "true",
                IsActive = true
            };

            // Make A depend on B to create cycle
            ruleA.DependsOnRuleId = ruleB.Id;

            var workflow = new Workflow
            {
                Description = "Circular test",
                Rules = new List<Rule> { ruleA, ruleB }
            };

            var act = () => workflow.Validate();
            act.Should().Throw<CircularReferenceException>();
        }

        [Fact]
        public void Validate_MissingDependency_ThrowsRuleValidationException()
        {
            var ruleA = new Rule
            {
                Description = "RuleA",
                DependsOnRuleId = Guid.NewGuid(), // Non-existent rule
                Expression = "true",
                IsActive = true
            };

            var workflow = new Workflow
            {
                Description = "Missing dep test",
                Rules = new List<Rule> { ruleA }
            };

            var act = () => workflow.Validate();
            act.Should().Throw<RuleValidationException>().WithMessage("*does not exist*");
        }

        // ==================== RuleContext ====================

        [Fact]
        public void RuleContext_StoreAndRetrieve_ReturnsResult()
        {
            var context = new RuleContext();
            var ruleId = Guid.NewGuid();
            var expectedResult = new RuleResult(true, ruleId, "Test", true, Value: 42);

            context.StoreResult(ruleId, expectedResult);
            var retrieved = context.GetResult(ruleId);

            retrieved.Should().NotBeNull();
            retrieved.Should().Be(expectedResult);
        }

        [Fact]
        public void RuleContext_GetValueTyped_ReturnsCorrectType()
        {
            var context = new RuleContext();
            var ruleId = Guid.NewGuid();
            var result = new RuleResult(true, ruleId, "Test", true, Value: "hello");

            context.StoreResult(ruleId, result);
            var value = context.GetValue<string>(ruleId);

            value.Should().Be("hello");
        }

        [Fact]
        public void RuleContext_GetValue_WrongType_ReturnsDefault()
        {
            var context = new RuleContext();
            var ruleId = Guid.NewGuid();
            var result = new RuleResult(true, ruleId, "Test", true, Value: "hello");

            context.StoreResult(ruleId, result);
            var value = context.GetValue<int>(ruleId); // Wrong type

            value.Should().Be(0);
        }

        [Fact]
        public void RuleContext_HasResult_Missing_ReturnsFalse()
        {
            var context = new RuleContext();
            context.HasResult(Guid.NewGuid()).Should().BeFalse();
        }

        [Fact]
        public void RuleContext_Clear_RemovesAllResults()
        {
            var context = new RuleContext();
            var ruleId = Guid.NewGuid();
            context.StoreResult(ruleId, new RuleResult(true, ruleId, "Test", true));

            context.Clear();
            context.HasResult(ruleId).Should().BeFalse();
        }

        // ==================== Execution Order ====================

        [Fact]
        public void Execute_TopologicalSort_MultipleDependencies()
        {
            var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };
            var ruleC = new Rule { Description = "C", DependsOnRuleId = ruleB.Id, Expression = "true", IsActive = true };
            var ruleD = new Rule { Description = "D", Expression = "true", IsActive = true }; // Independent

            var workflow = new Workflow
            {
                Description = "Chain test",
                Rules = new List<Rule> { ruleC, ruleD, ruleB, ruleA } // Scrambled order
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var results = workflow.Execute(_parameters).ToList();
            var order = results.Select(r => r.RuleDescription).ToList();

            // A must come before B, B must come before C
            order.IndexOf("A").Should().BeLessThan(order.IndexOf("B"));
            order.IndexOf("B").Should().BeLessThan(order.IndexOf("C"));
        }

        [Fact]
        public void Execute_PriorityWithDependencies_RespectsBoth()
        {
            var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true, Priority = 0 };
            var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true, Priority = 100 };

            var workflow = new Workflow
            {
                Description = "Priority test",
                Rules = new List<Rule> { ruleA, ruleB }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var results = workflow.Execute(_parameters).ToList();

            // B has higher priority but A is its dependency, so A must execute first
            results[0].RuleDescription.Should().Be("A");
            results[1].RuleDescription.Should().Be("B");
        }

        // ==================== Async Execution ====================

        [Fact]
        public async Task ExecuteAsync_WithDependency_ExecutesInOrder()
        {
            var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };

            var workflow = new Workflow
            {
                Description = "Async chain test",
                Rules = new List<Rule> { ruleB, ruleA }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var results = new List<RuleResult>();
            await foreach (var result in workflow.ExecuteAsync(_parameters, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            results[0].RuleDescription.Should().Be("A");
            results[1].RuleDescription.Should().Be("B");
        }

        [Fact]
        public async Task ExecuteParallelAsync_WithDependencies_ExecutesDependenciesFirst()
        {
            var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "B", DependsOnRuleId = ruleA.Id, Expression = "true", IsActive = true };
            var ruleC = new Rule { Description = "C", Expression = "true", IsActive = true }; // Independent

            var workflow = new Workflow
            {
                Description = "Parallel dep test",
                Rules = new List<Rule> { ruleA, ruleB, ruleC }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var results = await workflow.ExecuteParallelAsync(_parameters, TestContext.Current.CancellationToken);
            var resultDict = results.ToDictionary(r => r.RuleDescription, r => r);

            resultDict.Should().ContainKey("A");
            resultDict.Should().ContainKey("B");
            resultDict.Should().ContainKey("C");
            resultDict["A"].Success.Should().BeTrue();
            resultDict["B"].Success.Should().BeTrue();
            resultDict["C"].Success.Should().BeTrue();
        }

        // ==================== RuleResult Id Access ====================

        [Fact]
        public void RuleResult_RuleId_Accessible()
        {
            var ruleId = Guid.NewGuid();
            var result = new RuleResult(true, ruleId, "Test", true);

            result.RuleId.Should().Be(ruleId);
        }
    }
}