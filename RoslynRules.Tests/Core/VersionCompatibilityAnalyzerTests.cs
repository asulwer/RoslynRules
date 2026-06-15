using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using RoslynRules.Versioning;
using System;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Core
{
    public class VersionCompatibilityAnalyzerTests
    {
        [Fact]
        public void Analyze_SameWorkflows_NoChanges()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.IsCompatible.Should().BeTrue();
            analysis.HasBreakingChanges.Should().BeFalse();
            analysis.RequiresMigration.Should().BeFalse();
            analysis.Changes.Should().BeEmpty();
        }

        [Fact]
        public void Analyze_AddedRule_Detected()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0"), ("rule2", "1.0.0") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.Changes.Should().ContainSingle(c => c.Type == ChangeType.Added);
            analysis.IsCompatible.Should().BeTrue();
        }

        [Fact]
        public void Analyze_RemovedRule_Detected()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0"), ("rule2", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.Changes.Should().ContainSingle(c => c.Type == ChangeType.Removed);
            analysis.IsCompatible.Should().BeTrue();
        }

        [Fact]
        public void Analyze_MinorBump_Detected()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "1.1.0") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.Changes.Should().ContainSingle(c => c.Type == ChangeType.MinorBump);
            analysis.IsCompatible.Should().BeTrue();
            analysis.HasBreakingChanges.Should().BeFalse();
        }

        [Fact]
        public void Analyze_MajorBump_Detected()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "2.0.0") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.Changes.Should().ContainSingle(c => c.Type == ChangeType.MajorBump);
            analysis.IsCompatible.Should().BeFalse();
            analysis.HasBreakingChanges.Should().BeTrue();
        }

        [Fact]
        public void Analyze_PatchBump_Detected()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.1") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.Changes.Should().ContainSingle(c => c.Type == ChangeType.PatchBump);
            analysis.IsCompatible.Should().BeTrue();
        }

        [Fact]
        public void Analyze_WorkflowVersionChanged_RequiresMigration()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("2.0.0", new[] { ("rule1", "1.0.0") });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.RequiresMigration.Should().BeTrue();
            analysis.IsCompatible.Should().BeFalse();
        }

        [Fact]
        public void ValidateCompatible_Compatible_DoesNotThrow()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });

            Action act = () => VersionCompatibilityAnalyzer.ValidateCompatible(current, target);
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateCompatible_Incompatible_Throws()
        {
            var current = CreateWorkflow("1.0.0", new[] { ("rule1", "1.0.0") });
            var target = CreateWorkflow("1.0.0", new[] { ("rule1", "2.0.0") });

            Action act = () => VersionCompatibilityAnalyzer.ValidateCompatible(current, target);
            act.Should().Throw<RuleValidationException>();
        }

        [Fact]
        public void Analyze_MultipleChanges_Detected()
        {
            var current = CreateWorkflow("1.0.0", new[]
            {
                ("rule1", "1.0.0"),
                ("rule2", "1.0.0"),
                ("rule3", "1.0.0")
            });

            var target = CreateWorkflow("1.0.0", new[]
            {
                ("rule1", "1.1.0"),   // minor bump
                ("rule2", "2.0.0"),   // major bump
                ("rule4", "1.0.0")    // added
            });

            var analysis = VersionCompatibilityAnalyzer.Analyze(current, target);

            analysis.Changes.Should().HaveCount(4);
            analysis.Changes.Should().Contain(c => c.Type == ChangeType.MinorBump);
            analysis.Changes.Should().Contain(c => c.Type == ChangeType.MajorBump);
            analysis.Changes.Should().Contain(c => c.Type == ChangeType.Added);
            analysis.Changes.Should().Contain(c => c.Type == ChangeType.Removed);
            analysis.HasBreakingChanges.Should().BeTrue();
        }

        private static Workflow CreateWorkflow(string version, (string id, string version)[] rules)
        {
            var workflow = new Workflow
            {
                Version = RuleVersion.Parse(version)
            };

            foreach (var (id, v) in rules)
            {
                // Use a deterministic hash-based GUID from the string id
                var bytes = System.Text.Encoding.UTF8.GetBytes(id.PadRight(16, '0'));
                var hash = System.Security.Cryptography.MD5.HashData(bytes);
                var ruleId = new Guid(hash);
                workflow.Rules.Add(new Rule(ruleId, RuleVersion.Parse(v)));
            }

            return workflow;
        }
    }
}
