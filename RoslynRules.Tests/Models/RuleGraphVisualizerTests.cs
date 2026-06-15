using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Models
{
    /// <summary>
    /// Tests for RuleGraphVisualizer DOT and Mermaid output generation.
    /// </summary>
    public class RuleGraphVisualizerTests
    {
        [Fact]
        public void ToDot_SingleRule_ReturnsValidDot()
        {
            var rule = new Rule { Description = "Adult check", Expression = "x >= 18", IsActive = true };
            var workflow = new Workflow { Rules = { rule } };

            var dot = RuleGraphVisualizer.ToDot(workflow);

            dot.Should().StartWith("digraph Rules {");
            dot.Should().Contain("Adult check");
            dot.Should().Contain(rule.Id.ToString("N"));
            dot.Should().Contain("}");
        }

        [Fact]
        public void ToDot_WithDependency_ReturnsDependencyEdge()
        {
            var ruleA = new Rule { Description = "Validate", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "Process", Expression = "true", IsActive = true, DependsOnRuleId = ruleA.Id };
            var workflow = new Workflow { Rules = { ruleA, ruleB } };

            var dot = RuleGraphVisualizer.ToDot(workflow);

            dot.Should().Contain("depends on");
            dot.Should().Contain($"\"{ruleA.Id:N}\" -> \"{ruleB.Id:N}\"");
        }

        [Fact]
        public void ToDot_WithChildRules_ReturnsChildEdges()
        {
            var parent = new Rule { Description = "Parent", Expression = "true", IsActive = true };
            var child = new Rule { Description = "Child", Expression = "true", IsActive = true };
            parent.ChildRules.Add(child);

            var workflow = new Workflow { Rules = { parent } };
            var dot = RuleGraphVisualizer.ToDot(workflow);

            dot.Should().Contain("child");
            dot.Should().Contain($"\"{parent.Id:N}\" -> \"{child.Id:N}\"");
        }

        [Fact]
        public void ToDot_InactiveRule_ExcludedByDefault()
        {
            var active = new Rule { Description = "Active", Expression = "true", IsActive = true };
            var inactive = new Rule { Description = "Inactive", Expression = "true", IsActive = false };
            var workflow = new Workflow { Rules = { active, inactive } };

            var dot = RuleGraphVisualizer.ToDot(workflow);

            dot.Should().Contain("Active");
            dot.Should().NotContain("Inactive");
        }

        [Fact]
        public void ToDot_IncludeInactive_ShowsInactive()
        {
            var active = new Rule { Description = "Active", Expression = "true", IsActive = true };
            var inactive = new Rule { Description = "Inactive", Expression = "true", IsActive = false };
            var workflow = new Workflow { Rules = { active, inactive } };

            var dot = RuleGraphVisualizer.ToDot(workflow, includeInactive: true);

            dot.Should().Contain("Active");
            dot.Should().Contain("Inactive");
        }

        [Fact]
        public void ToDot_EscapesQuotes()
        {
            var rule = new Rule { Description = "Check \"Adult\"", Expression = "true", IsActive = true };
            var workflow = new Workflow { Rules = { rule } };

            var dot = RuleGraphVisualizer.ToDot(workflow);

            dot.Should().Contain("Check \\\"Adult\\\"");
        }

        [Fact]
        public void ToMermaid_SingleRule_ReturnsValidMermaid()
        {
            var rule = new Rule { Description = "Adult check", Expression = "x >= 18", IsActive = true };
            var workflow = new Workflow { Rules = { rule } };

            var mermaid = RuleGraphVisualizer.ToMermaid(workflow);

            mermaid.Should().StartWith("graph TD");
            mermaid.Should().Contain("Adult check");
            mermaid.Should().Contain($"R{rule.Id:N}");
        }

        [Fact]
        public void ToMermaid_WithDependency_ReturnsDependencyEdge()
        {
            var ruleA = new Rule { Description = "Validate", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "Process", Expression = "true", IsActive = true, DependsOnRuleId = ruleA.Id };
            var workflow = new Workflow { Rules = { ruleA, ruleB } };

            var mermaid = RuleGraphVisualizer.ToMermaid(workflow);

            mermaid.Should().Contain("depends on");
            mermaid.Should().Contain($"R{ruleA.Id:N}");
            mermaid.Should().Contain($"R{ruleB.Id:N}");
        }

        [Fact]
        public void ToMermaid_WithChildRules_ReturnsChildEdges()
        {
            var parent = new Rule { Description = "Parent", Expression = "true", IsActive = true };
            var child = new Rule { Description = "Child", Expression = "true", IsActive = true };
            parent.ChildRules.Add(child);

            var workflow = new Workflow { Rules = { parent } };
            var mermaid = RuleGraphVisualizer.ToMermaid(workflow);

            mermaid.Should().Contain("child");
            mermaid.Should().Contain($"R{parent.Id:N}");
            mermaid.Should().Contain($"R{child.Id:N}");
        }

        [Fact]
        public void ToMermaid_InactiveRule_StyledDifferently()
        {
            var active = new Rule { Description = "Active", Expression = "true", IsActive = true };
            var inactive = new Rule { Description = "Inactive", Expression = "true", IsActive = false };
            var workflow = new Workflow { Rules = { active, inactive } };

            var mermaid = RuleGraphVisualizer.ToMermaid(workflow);

            mermaid.Should().Contain("Active");
            mermaid.Should().NotContain("Inactive");
        }

        [Fact]
        public void ToMermaid_IncludeInactive_HasInactiveClass()
        {
            var active = new Rule { Description = "Active", Expression = "true", IsActive = true };
            var inactive = new Rule { Description = "Inactive", Expression = "true", IsActive = false };
            var workflow = new Workflow { Rules = { active, inactive } };

            var mermaid = RuleGraphVisualizer.ToMermaid(workflow, includeInactive: true);

            mermaid.Should().Contain(":::inactive");
            mermaid.Should().Contain("classDef inactive");
        }

        [Fact]
        public void ToDot_SingleRuleOverload_Works()
        {
            var rule = new Rule { Description = "Root", Expression = "true", IsActive = true };
            var child = new Rule { Description = "Child", Expression = "true", IsActive = true };
            rule.ChildRules.Add(child);

            var dot = RuleGraphVisualizer.ToDot(rule);

            dot.Should().Contain("Root");
            dot.Should().Contain("Child");
        }

        [Fact]
        public void ToMermaid_SingleRuleOverload_Works()
        {
            var rule = new Rule { Description = "Root", Expression = "true", IsActive = true };
            var child = new Rule { Description = "Child", Expression = "true", IsActive = true };
            rule.ChildRules.Add(child);

            var mermaid = RuleGraphVisualizer.ToMermaid(rule);

            mermaid.Should().Contain("Root");
            mermaid.Should().Contain("Child");
        }

        [Fact]
        public void ToDot_ComplexWorkflow_HasAllRelationships()
        {
            var ruleA = new Rule { Description = "A", Expression = "true", IsActive = true };
            var ruleB = new Rule { Description = "B", Expression = "true", IsActive = true, DependsOnRuleId = ruleA.Id };
            var ruleC = new Rule { Description = "C", Expression = "true", IsActive = true };
            ruleB.ChildRules.Add(ruleC);
            var workflow = new Workflow { Rules = { ruleA, ruleB } };

            var dot = RuleGraphVisualizer.ToDot(workflow);

            dot.Should().Contain("depends on"); // A <- B
            dot.Should().Contain("child");    // B <- C
            dot.Should().Contain("A");
            dot.Should().Contain("B");
            dot.Should().Contain("C");
        }

        [Fact]
        public void ToMermaid_EscapesBrackets()
        {
            var rule = new Rule { Description = "Check [value]", Expression = "true", IsActive = true };
            var workflow = new Workflow { Rules = { rule } };

            var mermaid = RuleGraphVisualizer.ToMermaid(workflow);

            mermaid.Should().NotContain("[value]"); // raw brackets would break mermaid
        }
    }
}
