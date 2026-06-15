using RoslynRules.Json;
using RoslynRules.Models;
using RoslynRules.Templates;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RoslynRules.Demo.Tests;

/// <summary>
/// Integration tests covering the Demo project scenarios.
/// Tests JSON round-tripping, template instantiation, and workflow/rule batch comparison.
/// </summary>
public class DemoIntegrationTests
{
    private static readonly RoslynRules.Compiler.AssemblyReferenceProvider ReferenceProvider;
    private static readonly RoslynRules.Compiler.ExpressionCompiler Compiler = new();

    static DemoIntegrationTests()
    {
        ReferenceProvider = new RoslynRules.Compiler.AssemblyReferenceProvider(
            RoslynRules.Compiler.AssemblyReferenceProvider.DefaultWhitelist
                .Concat(new[] { "RoslynRules.Demo.Tests", "RoslynRules.Tests" }));
    }

    // ==================== JSON ROUND-TRIP ====================

    [Fact]
    public void JsonRoundTrip_Workflow_SerializesAndDeserializesCorrectly()
    {
        var original = new Workflow
        {
            Description = "JSON test",
            Rules = new List<Rule>
            {
                new() { Description = "Active check", Expression = "customer.IsActive == true" }
            }
        };

        var json = JsonRuleLoader.Serialize(original);
        var restored = JsonRuleLoader.DeserializeWorkflow(json);

        restored.Should().NotBeNull();
        restored.Description.Should().Be("JSON test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Expression.Should().Be("customer.IsActive == true");
    }

    [Fact]
    public void JsonRoundTrip_Workflow_ExecutesAfterRestore()
    {
        var wf = new Workflow
        {
            Description = "JSON test",
            Rules = new List<Rule>
            {
                new() { Description = "Active check", Expression = "customer.IsActive == true" }
            }
        };

        var json = JsonRuleLoader.Serialize(wf);
        var restored = JsonRuleLoader.DeserializeWorkflow(json);

        var compileParam = new RuleParameter("customer", typeof(DemoTestCustomer));
        restored.Compile(new[] { compileParam }, null, ReferenceProvider);

        var customer = new DemoTestCustomer { IsActive = true };
        var param = new RuleParameter("customer", typeof(DemoTestCustomer), customer);
        var results = restored.Execute(param).ToArray();

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
    }

    // ==================== TEMPLATE INSTANTIATION ====================

    [Fact]
    public void TemplateInstantiation_AgeThreshold_InstantiatesAndExecutes()
    {
        var template = new RuleTemplate
        {
            Description = "Age threshold check",
            Expression = "customer.Age >= {minAge}"
        };
        template.Placeholders.Add("minAge", PlaceholderKind.Value);

        var values = new Dictionary<string, object> { ["minAge"] = 21 };
        var compileParam = new RuleParameter("customer", typeof(DemoTestCustomer));
        var param = new RuleParameter("customer", typeof(DemoTestCustomer), new DemoTestCustomer { Age = 25 });

        var rule = template.Instantiate(values, Compiler, new[] { compileParam }, Array.Empty<string>(), ReferenceProvider);
        var result = rule.Execute(param);

        result.Success.Should().BeTrue();
        rule.Description.Should().Be("Age threshold check");
    }

    [Fact]
    public void TemplateInstantiation_AgeThreshold_FailsForYounger()
    {
        var template = new RuleTemplate
        {
            Description = "Age threshold check",
            Expression = "customer.Age >= {minAge}"
        };
        template.Placeholders.Add("minAge", PlaceholderKind.Value);

        var values = new Dictionary<string, object> { ["minAge"] = 21 };
        var compileParam = new RuleParameter("customer", typeof(DemoTestCustomer));
        var param = new RuleParameter("customer", typeof(DemoTestCustomer), new DemoTestCustomer { Age = 18 });

        var rule = template.Instantiate(values, Compiler, new[] { compileParam }, Array.Empty<string>(), ReferenceProvider);
        var result = rule.Execute(param);

        result.Success.Should().BeFalse();
    }

    // ==================== WORKFLOW VS RULE BATCH ====================

    [Fact]
    public void WorkflowVsRuleBatch_SequentialAndParallel_ReturnSameResults()
    {
        var wf = new Workflow
        {
            Description = "Batch comparison",
            Rules = new List<Rule>
            {
                new() { Description = "Rule 1", Expression = "customer.Age >= 18" },
                new() { Description = "Rule 2", Expression = "customer.IsActive == true" },
                new() { Description = "Rule 3", Expression = "customer.Tags.Count > 0" }
            }
        };

        var customer = new DemoTestCustomer { Age = 25, IsActive = true, Tags = new List<string> { "tag1" } };
        var compileParam = new RuleParameter("customer", typeof(DemoTestCustomer));
        wf.Compile(new[] { compileParam }, null, ReferenceProvider);

        var param = new RuleParameter("customer", typeof(DemoTestCustomer), customer);

        var seq = wf.Execute(param).ToArray();
        var par = wf.ExecuteParallel(param);

        seq.Should().HaveCount(3);
        par.Should().HaveCount(3);

        for (int i = 0; i < seq.Length; i++)
        {
            seq[i].Success.Should().Be(par[i].Success);
        }
    }

    [Fact]
    public void WorkflowVsRuleBatch_AllRulesPass_WithValidCustomer()
    {
        var wf = new Workflow
        {
            Description = "Batch comparison",
            Rules = new List<Rule>
            {
                new() { Description = "Rule 1", Expression = "customer.Age >= 18" },
                new() { Description = "Rule 2", Expression = "customer.IsActive == true" },
                new() { Description = "Rule 3", Expression = "customer.Tags.Count > 0" }
            }
        };

        var customer = new DemoTestCustomer { Age = 25, IsActive = true, Tags = new List<string> { "tag1" } };
        var compileParam = new RuleParameter("customer", typeof(DemoTestCustomer));
        wf.Compile(new[] { compileParam }, null, ReferenceProvider);

        var param = new RuleParameter("customer", typeof(DemoTestCustomer), customer);
        var results = wf.Execute(param).ToArray();

        results.Should().HaveCount(3);
        results.All(r => r.Success).Should().BeTrue();
    }

    // ==================== MULTI-PARAMETER DEMO ====================

    [Fact]
    public void MultiParameter_TwoParams_ExecutesSuccessfully()
    {
        var rule = new Rule
        {
            Description = "Price check",
            Expression = "price > 0 && quantity > 0",
            IsActive = true
        };

        var compileParams = new[]
        {
            new RuleParameter("price", typeof(decimal)),
            new RuleParameter("quantity", typeof(int))
        };
        rule.Compile(Compiler, compileParams);

        var executeParams = new[]
        {
            new RuleParameter("price", typeof(decimal), 9.99m),
            new RuleParameter("quantity", typeof(int), 5)
        };
        var result = rule.Execute(executeParams);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void MultiParameter_WrongCount_ThrowsArgumentException()
    {
        var rule = new Rule
        {
            Description = "Price check",
            Expression = "price > 0 && quantity > 0",
            IsActive = true
        };

        var compileParams = new[]
        {
            new RuleParameter("price", typeof(decimal)),
            new RuleParameter("quantity", typeof(int))
        };
        rule.Compile(Compiler, compileParams);

        var wrongCount = new[]
        {
            new RuleParameter("price", typeof(decimal), 5.00m)
        };

        Action act = () => rule.Execute(wrongCount);
        act.Should().Throw<RoslynRules.Exceptions.RuleValidationException>();
    }

    // ==================== HELPER MODELS ====================

    /// <summary>
    /// Minimal customer model for demo tests without external dependencies.
    /// </summary>
    public class DemoTestCustomer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public List<Order> Orders { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    public class Order
    {
        public int Id { get; set; }
        public double Total { get; set; }
        public List<string> Items { get; set; } = new();
    }
}
