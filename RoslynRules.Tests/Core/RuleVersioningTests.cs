using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using Xunit;

namespace RoslynRules.Tests.Core
{
    public class RuleVersioningTests
    {
        [Fact]
        public void Rule_DefaultVersion_Is1_0_0()
        {
            var rule = new Rule();
            rule.Version.Should().Be(new RuleVersion(1, 0, 0));
        }

        [Fact]
        public void Rule_CreatedAt_IsUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var rule = new Rule();
            var after = DateTime.UtcNow.AddSeconds(1);

            rule.CreatedAt.Should().BeOnOrAfter(before);
            rule.CreatedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Rule_BumpMajorVersion_IncrementsMajor()
        {
            var rule = new Rule();
            rule.BumpMajorVersion();
            rule.Version.Should().Be(new RuleVersion(2, 0, 0));
        }

        [Fact]
        public void Rule_BumpMinorVersion_IncrementsMinor()
        {
            var rule = new Rule();
            rule.BumpMinorVersion();
            rule.Version.Should().Be(new RuleVersion(1, 1, 0));
        }

        [Fact]
        public void Rule_BumpPatchVersion_IncrementsPatch()
        {
            var rule = new Rule();
            rule.BumpPatchVersion();
            rule.Version.Should().Be(new RuleVersion(1, 0, 1));
        }

        [Fact]
        public void Rule_BumpVersion_UpdatesModifiedAt()
        {
            var rule = new Rule();
            var before = DateTime.UtcNow.AddSeconds(-1);
            rule.BumpPatchVersion();
            var after = DateTime.UtcNow.AddSeconds(1);

            rule.ModifiedAt.Should().BeOnOrAfter(before);
            rule.ModifiedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Rule_BumpVersion_SetsModifiedBy()
        {
            var rule = new Rule();
            rule.BumpPatchVersion("test-user");
            rule.ModifiedBy.Should().Be("test-user");
        }

        [Fact]
        public void Rule_Constructor_WithVersion_SetsVersion()
        {
            var id = Guid.NewGuid();
            var version = new RuleVersion(2, 1, 0);
            var rule = new Rule(id, version);

            rule.Id.Should().Be(id);
            rule.Version.Should().Be(version);
        }

        [Fact]
        public void Workflow_DefaultVersion_Is1_0_0()
        {
            var workflow = new Workflow();
            workflow.Version.Should().Be(new RuleVersion(1, 0, 0));
        }

        [Fact]
        public void Workflow_CreatedAt_IsUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var workflow = new Workflow();
            var after = DateTime.UtcNow.AddSeconds(1);

            workflow.CreatedAt.Should().BeOnOrAfter(before);
            workflow.CreatedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Workflow_BumpMajorVersion_IncrementsMajor()
        {
            var workflow = new Workflow();
            workflow.BumpMajorVersion();
            workflow.Version.Should().Be(new RuleVersion(2, 0, 0));
        }

        [Fact]
        public void Workflow_BumpMinorVersion_IncrementsMinor()
        {
            var workflow = new Workflow();
            workflow.BumpMinorVersion();
            workflow.Version.Should().Be(new RuleVersion(1, 1, 0));
        }

        [Fact]
        public void Workflow_BumpPatchVersion_IncrementsPatch()
        {
            var workflow = new Workflow();
            workflow.BumpPatchVersion();
            workflow.Version.Should().Be(new RuleVersion(1, 0, 1));
        }

        [Fact]
        public void Workflow_BumpVersion_UpdatesModifiedAt()
        {
            var workflow = new Workflow();
            var before = DateTime.UtcNow.AddSeconds(-1);
            workflow.BumpPatchVersion();
            var after = DateTime.UtcNow.AddSeconds(1);

            workflow.ModifiedAt.Should().BeOnOrAfter(before);
            workflow.ModifiedAt.Should().BeOnOrBefore(after);
        }

        [Fact]
        public void Workflow_BumpVersion_SetsModifiedBy()
        {
            var workflow = new Workflow();
            workflow.BumpPatchVersion("test-user");
            workflow.ModifiedBy.Should().Be("test-user");
        }

        [Fact]
        public void Workflow_GetRuleVersions_ReturnsAllRuleVersions()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Version = new RuleVersion(1, 0, 0) },
                    new Rule
                    {
                        Version = new RuleVersion(2, 0, 0),
                        ChildRules =
                        {
                            new Rule { Version = new RuleVersion(1, 5, 0) }
                        }
                    }
                }
            };

            var versions = workflow.GetRuleVersions();
            versions.Should().HaveCount(3);
        }

        [Fact]
        public void Workflow_IsVersionCompatibleWith_SameVersion_ReturnsTrue()
        {
            var workflow1 = new Workflow { Version = new RuleVersion(1, 0, 0) };
            var workflow2 = new Workflow { Version = new RuleVersion(1, 0, 0) };

            workflow1.IsVersionCompatibleWith(workflow2).Should().BeTrue();
        }

        [Fact]
        public void Workflow_IsVersionCompatibleWith_HigherMinor_ReturnsTrue()
        {
            var workflow1 = new Workflow { Version = new RuleVersion(1, 1, 0) };
            var workflow2 = new Workflow { Version = new RuleVersion(1, 0, 0) };

            workflow1.IsVersionCompatibleWith(workflow2).Should().BeTrue();
        }

        [Fact]
        public void Workflow_IsVersionCompatibleWith_DifferentMajor_ReturnsFalse()
        {
            var workflow1 = new Workflow { Version = new RuleVersion(2, 0, 0) };
            var workflow2 = new Workflow { Version = new RuleVersion(1, 0, 0) };

            workflow1.IsVersionCompatibleWith(workflow2).Should().BeFalse();
        }

        [Fact]
        public void Rule_Version_CannotChangeAfterCompile()
        {
            var compiler = new RoslynRules.Compiler.ExpressionCompiler();
            var rule = new Rule
            {
                Expression = "true",
                Version = new RuleVersion(1, 0, 0)
            };

            rule.Compile(compiler, new[] { new RuleParameter("x", typeof(bool)) });

            Action act = () => rule.BumpPatchVersion();
            act.Should().Throw<RuleCompilationException>();
        }

        [Fact]
        public void Workflow_Version_CannotChangeAfterCompile()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Expression = "true" }
                }
            };

            workflow.Compile(new[] { new RuleParameter("x", typeof(bool)) });

            Action act = () => workflow.BumpPatchVersion();
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
