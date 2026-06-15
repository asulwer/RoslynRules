using FluentAssertions;
using RoslynRules.Abstractions;
using RoslynRules.Batch;
using RoslynRules.Models;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Models
{
    /// <summary>
    /// Verifies that Workflow and RuleBatch correctly implement IRuleEngine
    /// when consumed through the interface (DI/mocking scenario).
    /// </summary>
    public class RuleEngineInterfaceTests
    {
        private readonly RuleParameter[] _compileParams = new[]
        {
            new RuleParameter("customer", typeof(TestCustomer))
        };

        private readonly RuleParameter[] _executeParams = new[]
        {
            new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25 })
        };

        // ==================== WORKFLOW ====================

        [Fact]
        public void Workflow_Implements_IRuleEngine()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            Assert.NotNull(engine);
        }

        [Fact]
        public void Workflow_ViaInterface_CompileAndExecute()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();

            var rule = new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            };
            ((Workflow)engine).Rules.Add(rule);

            engine.Compile(_compileParams);
            var results = engine.Execute(_executeParams).ToArray();

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        [Fact]
        public void Workflow_ViaInterface_MutateRulesAfterCompile_Throws()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            ((Workflow)engine).Rules.Add(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);

            // ReadOnlyCollection throws NotSupportedException for mutation attempts
            var ex = Assert.Throws<NotSupportedException>(() =>
                ((Workflow)engine).Rules.Add(new Rule { Expression = "true" }));
            Assert.Contains("read-only", ex.Message);
        }

        [Fact]
        public void Workflow_ViaInterface_SetRulesAfterCompile_Throws()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            ((Workflow)engine).Rules.Add(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ((Workflow)engine).Rules = new List<Rule>());
            Assert.Contains("cannot be modified after compilation", ex.Message);
        }

        [Fact]
        public void Workflow_ViaInterface_ReadRulesAfterCompile_ReturnsReadOnly()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            ((Workflow)engine).Rules.Add(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);

            var rules = ((Workflow)engine).Rules;
            rules.Should().BeOfType<System.Collections.ObjectModel.ReadOnlyCollection<Rule>>();
        }

        [Fact]
        public void Workflow_ViaInterface_ExecuteParallel()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            var rule = new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            };
            ((Workflow)engine).Rules.Add(rule);

            engine.Compile(_compileParams);
            var results = engine.ExecuteParallel(_executeParams);

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        [Fact]
        public async Task Workflow_ViaInterface_ExecuteAsync()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            var rule = new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            };
            ((Workflow)engine).Rules.Add(rule);

            engine.Compile(_compileParams);
            var results = new List<RuleResult>();
            await foreach (var result in engine.ExecuteAsync(_executeParams, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        [Fact]
        public async Task Workflow_ViaInterface_ExecuteParallelAsync()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            var rule = new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            };
            ((Workflow)engine).Rules.Add(rule);

            engine.Compile(_compileParams);
            var results = await engine.ExecuteParallelAsync(_executeParams, TestContext.Current.CancellationToken);

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        // ==================== RULE BATCH ====================

        [Fact]
        public void RuleBatch_Implements_IRuleEngine()
        {
            IRuleEngine engine = new RuleBatch();
            Assert.NotNull(engine);
        }

        [Fact]
        public void RuleBatch_ViaInterface_CompileAndExecute()
        {
            IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);
            var results = engine.Execute(_executeParams).ToArray();

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        [Fact]
        public void RuleBatch_ViaInterface_ExecuteParallel()
        {
            IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);
            var results = engine.ExecuteParallel(_executeParams);

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        [Fact]
        public async Task RuleBatch_ViaInterface_ExecuteAsync()
        {
            IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);
            var results = new List<RuleResult>();
            await foreach (var result in engine.ExecuteAsync(_executeParams, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        [Fact]
        public async Task RuleBatch_ViaInterface_ExecuteParallelAsync()
        {
            IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            engine.Compile(_compileParams);
            var results = await engine.ExecuteParallelAsync(_executeParams, TestContext.Current.CancellationToken);

            Assert.Single(results);
            Assert.True(results[0].Success);
        }

        // ==================== VALIDATION ====================

        [Fact]
        public void Workflow_ViaInterface_Validate_ThrowsOnEmpty()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            Assert.Throws<Exceptions.WorkflowException>(() => engine.Validate());
        }

        [Fact]
        public void RuleBatch_ViaInterface_Validate_ThrowsOnEmpty()
        {
            IRuleEngine engine = new RuleBatch();
            Assert.Throws<Exceptions.WorkflowException>(() => engine.Validate());
        }

        [Fact]
        public void Workflow_ViaInterface_ExecuteWithoutCompile_Throws()
        {
            IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            ((Workflow)engine).Rules.Add(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            Assert.Throws<Exceptions.NotCompiledException>(() => engine.Execute(_executeParams).ToArray());
        }

        [Fact]
        public void RuleBatch_ViaInterface_ExecuteWithoutCompile_Throws()
        {
            IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule
            {
                Expression = "customer.Age >= 18",
                Description = "Age check"
            });

            Assert.Throws<Exceptions.NotCompiledException>(() => engine.Execute(_executeParams).ToArray());
        }
    }
}