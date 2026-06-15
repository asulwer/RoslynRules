using FluentAssertions;
using RoslynRules.Models;
using RoslynRules.Xml;
using System;
using Xunit;

namespace RoslynRules.Tests.Extensions
{
    /// <summary>
    /// Tests for XmlRuleLoader versioning and timestamp serialization.
    /// Validates that RuleVersion, DateTime, and ModifiedBy round-trip correctly.
    /// </summary>
    public class XmlVersioningTests
    {
        [Fact]
        public void Serialize_RuleVersion_SerializesCorrectly()
        {
            var rule = new Rule
            {
                Description = "Test rule",
                Version = new RuleVersion(2, 1, 3, "alpha", "build.123"),
                Expression = "true"
            };

            var xml = XmlRuleLoader.Serialize(rule);
            xml.Should().Contain("Version=\"2.1.3-alpha+build.123\"");
        }

        [Fact]
        public void Deserialize_RuleVersion_ParsesCorrectly()
        {
            var xml = "<Rule Id=\"11111111-1111-1111-1111-111111111111\" Version=\"2.1.3-alpha+build.123\" CreatedAt=\"2024-01-01T00:00:00.0000000Z\" ModifiedAt=\"2024-01-01T00:00:00.0000000Z\" IsActive=\"true\" Priority=\"0\"><Description>Test rule</Description><Expression>true</Expression></Rule>";

            var rule = XmlRuleLoader.DeserializeRule(xml);
            rule.Version.Should().Be(new RuleVersion(2, 1, 3, "alpha", "build.123"));
        }

        [Fact]
        public void Serialize_WorkflowVersion_SerializesCorrectly()
        {
            var workflow = new Workflow
            {
                Description = "Test workflow",
                Version = new RuleVersion(3, 0, 0),
                Rules =
                {
                    new Rule { Description = "R1", Expression = "true" }
                }
            };

            var xml = XmlRuleLoader.Serialize(workflow);
            xml.Should().Contain("Version=\"3.0.0\"");
        }

        [Fact]
        public void RoundTrip_Workflow_PreservesVersion()
        {
            var original = new Workflow
            {
                Description = "Test workflow",
                Version = new RuleVersion(2, 1, 0),
                Rules =
                {
                    new Rule
                    {
                        Description = "Test rule",
                        Version = new RuleVersion(1, 5, 2),
                        Expression = "true"
                    }
                }
            };

            var xml = XmlRuleLoader.Serialize(original);
            var restored = XmlRuleLoader.DeserializeWorkflow(xml);

            restored.Version.Should().Be(original.Version);
            restored.Rules[0].Version.Should().Be(original.Rules[0].Version);
        }

        [Fact]
        public void RoundTrip_Workflow_PreservesTimestamps()
        {
            var created = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var modified = new DateTime(2024, 6, 1, 14, 45, 0, DateTimeKind.Utc);

            var original = new Workflow
            {
                Description = "Test workflow",
                CreatedAt = created,
                ModifiedAt = modified,
                ModifiedBy = "test-user",
                Rules =
                {
                    new Rule
                    {
                        Description = "Test rule",
                        Expression = "true",
                        ModifiedBy = "another-user"
                    }
                }
            };

            var xml = XmlRuleLoader.Serialize(original);
            var restored = XmlRuleLoader.DeserializeWorkflow(xml);

            restored.CreatedAt.Should().Be(created);
            restored.ModifiedAt.Should().Be(modified);
            restored.ModifiedBy.Should().Be("test-user");
            restored.Rules[0].ModifiedBy.Should().Be("another-user");
        }

        [Fact]
        public void RoundTrip_Rule_PreservesTimestamps()
        {
            var created = new DateTime(2024, 3, 10, 8, 15, 0, DateTimeKind.Utc);
            var modified = new DateTime(2024, 7, 20, 16, 30, 0, DateTimeKind.Utc);

            var original = new Rule
            {
                Description = "Timestamp rule",
                Expression = "true",
                CreatedAt = created,
                ModifiedAt = modified,
                ModifiedBy = "rule-author"
            };

            var xml = XmlRuleLoader.Serialize(original);
            var restored = XmlRuleLoader.DeserializeRule(xml);

            restored.CreatedAt.Should().Be(created);
            restored.ModifiedAt.Should().Be(modified);
            restored.ModifiedBy.Should().Be("rule-author");
        }

        [Fact]
        public void RoundTrip_Workflow_WithPrereleaseVersion()
        {
            var original = new Workflow
            {
                Description = "Prerelease workflow",
                Version = new RuleVersion(1, 0, 0, "beta.2"),
                Rules =
                {
                    new Rule
                    {
                        Description = "Prerelease rule",
                        Version = new RuleVersion(0, 9, 0, "alpha"),
                        Expression = "true"
                    }
                }
            };

            var xml = XmlRuleLoader.Serialize(original);
            var restored = XmlRuleLoader.DeserializeWorkflow(xml);

            restored.Version.Should().Be(new RuleVersion(1, 0, 0, "beta.2"));
            restored.Rules[0].Version.Should().Be(new RuleVersion(0, 9, 0, "alpha"));
        }
    }
}
