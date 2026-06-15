using FluentAssertions;
using RoslynRules.Json;
using RoslynRules.Models;
using System;
using Xunit;

namespace RoslynRules.Tests.Extensions
{
    /// <summary>
    /// Tests for JsonRuleLoader serialization and deserialization.
    /// </summary>
    public class JsonRuleLoaderTests
    {
        [Fact]
        public void Serialize_Deserialize_PreservesCacheDuration()
        {
            var workflow = new Workflow
            {
                Description = "Test workflow",
                Rules =
                {
                    new Rule
                    {
                        Description = "Cached rule",
                        Expression = "true",
                        IsActive = true,
                        CacheDuration = TimeSpan.FromMinutes(5)
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(workflow);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules.Should().HaveCount(1);
            restored.Rules[0].CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void Serialize_Deserialize_PreservesTimeout()
        {
            var workflow = new Workflow
            {
                Description = "Test workflow",
                Rules =
                {
                    new Rule
                    {
                        Description = "Timed rule",
                        Expression = "true",
                        IsActive = true,
                        Timeout = TimeSpan.FromSeconds(10)
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(workflow);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules.Should().HaveCount(1);
            restored.Rules[0].Timeout.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Serialize_Deserialize_PreservesRuleProperties()
        {
            var workflow = new Workflow
            {
                Description = "Test workflow",
                IsActive = true,
                Rules =
                {
                    new Rule
                    {
                        Description = "Full rule",
                        Expression = "customer.Age >= 18",
                        Action = "customer.IsAdult = true",
                        IsActive = true,
                        Priority = 10,
                        CacheDuration = TimeSpan.FromMinutes(5),
                        Timeout = TimeSpan.FromSeconds(10)
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(workflow);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Description.Should().Be("Test workflow");
            restored.IsActive.Should().BeTrue();
            restored.Rules.Should().HaveCount(1);

            var rule = restored.Rules[0];
            rule.Description.Should().Be("Full rule");
            rule.Expression.Should().Be("customer.Age >= 18");
            rule.Action.Should().Be("customer.IsAdult = true");
            rule.IsActive.Should().BeTrue();
            rule.Priority.Should().Be(10);
            rule.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
            rule.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void Serialize_Deserialize_NullCacheDuration_RoundTrips()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule
                    {
                        Description = "No cache",
                        Expression = "true"
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(workflow);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules[0].CacheDuration.Should().BeNull();
        }

        [Fact]
        public void Serialize_Deserialize_ChildRules_PreserveCacheDuration()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true"
            };

            var child = new Rule
            {
                Description = "Child",
                Expression = "true",
                CacheDuration = TimeSpan.FromSeconds(30)
            };

            parent.ChildRules.Add(child);

            var workflow = new Workflow
            {
                Rules = { parent }
            };

            var json = JsonRuleLoader.Serialize(workflow);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules[0].ChildRules.Should().HaveCount(1);
            restored.Rules[0].ChildRules[0].CacheDuration.Should().Be(TimeSpan.FromSeconds(30));
        }
    }
}
