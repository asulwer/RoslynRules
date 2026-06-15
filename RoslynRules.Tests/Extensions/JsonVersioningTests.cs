using FluentAssertions;
using RoslynRules.Json;
using RoslynRules.Models;
using System;
using Xunit;

namespace RoslynRules.Tests.Extensions
{
    public class JsonVersioningTests
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

            var json = JsonRuleLoader.Serialize(rule);
            // Note: System.Text.Json escapes "+" as "\u002B" for security (to prevent breaking out of HTML contexts)
            // The serializer may produce "\u002B" or "+" depending on encoder settings
            json.Should().Contain("\"version\": \"2.1.3-alpha").And.Contain("build.123\"");
        }

        [Fact]
        public void Deserialize_RuleVersion_ParsesCorrectly()
        {
            var json = "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"description\":\"Test rule\",\"version\":\"2.1.3-alpha+build.123\",\"expression\":\"true\",\"isActive\":true,\"priority\":0}";

            var rule = JsonRuleLoader.DeserializeRule(json);
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

            var json = JsonRuleLoader.Serialize(workflow);
            json.Should().Contain("\"version\": \"3.0.0\"");
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

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

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

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.CreatedAt.Should().Be(created);
            restored.ModifiedAt.Should().Be(modified);
            restored.ModifiedBy.Should().Be("test-user");
        }

        [Fact]
        public void AotSerialize_RuleVersion_SerializesCorrectly()
        {
            var rule = new Rule
            {
                Description = "Test rule",
                Version = new RuleVersion(1, 2, 3),
                Expression = "true"
            };

            var json = JsonRuleLoader.SerializeAot(rule);
            json.Should().Contain("\"version\": \"1.2.3\"");
        }

        [Fact]
        public void AotRoundTrip_Workflow_PreservesVersion()
        {
            var original = new Workflow
            {
                Description = "Test workflow",
                Version = new RuleVersion(2, 0, 0),
                Rules =
                {
                    new Rule
                    {
                        Description = "Test rule",
                        Version = new RuleVersion(1, 1, 0),
                        Expression = "true"
                    }
                }
            };

            var json = JsonRuleLoader.SerializeAot(original);
            var restored = JsonRuleLoader.DeserializeWorkflowAot(json);

            restored.Version.Should().Be(original.Version);
            restored.Rules[0].Version.Should().Be(original.Rules[0].Version);
        }
    }
}
