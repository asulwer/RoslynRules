using FluentAssertions;
using RoslynRules.Models;
using RoslynRules.Snapshots;
using RoslynRules.Xml;
using System;
using Xunit;

namespace RoslynRules.Tests.Extensions;

/// <summary>
/// Tests for XmlRuleLoader serialization and deserialization.
/// </summary>
public class XmlRuleLoaderTests
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

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

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

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

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

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

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

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

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

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules[0].ChildRules.Should().HaveCount(1);
        restored.Rules[0].ChildRules[0].CacheDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesVersion()
    {
        var workflow = new Workflow
        {
            Version = new RuleVersion(2, 1, 0),
            Rules =
            {
                new Rule
                {
                    Version = new RuleVersion(1, 5, 3),
                    Description = "Versioned rule",
                    Expression = "true"
                }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Version.Should().Be(new RuleVersion(2, 1, 0));
        restored.Rules[0].Version.Should().Be(new RuleVersion(1, 5, 3));
    }

    [Fact]
    public void Serialize_Deserialize_PreservesDependencies()
    {
        var depId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var workflow = new Workflow
        {
            Rules =
            {
                new Rule { Id = depId, Description = "Dep", Expression = "true", IsActive = true },
                new Rule { Id = ruleId, Description = "Dependent", Expression = "true", IsActive = true, DependsOnRuleId = depId }
            }
        };

        var xml = XmlRuleLoader.Serialize(workflow);
        var restored = XmlRuleLoader.DeserializeWorkflow(xml);

        restored.Rules.Should().HaveCount(2);
        restored.Rules[1].DependsOnRuleId.Should().Be(depId);
    }

    [Fact]
    public void Serialize_SingleRule_RoundTrips()
    {
        var rule = new Rule
        {
            Description = "Standalone rule",
            Expression = "x > 0",
            Action = "result = x",
            Priority = 5
        };

        var xml = XmlRuleLoader.Serialize(rule);
        var restored = XmlRuleLoader.DeserializeRule(xml);

        restored.Description.Should().Be("Standalone rule");
        restored.Expression.Should().Be("x > 0");
        restored.Action.Should().Be("result = x");
        restored.Priority.Should().Be(5);
    }

    [Fact]
    public void SaveAndLoad_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var workflow = new Workflow
            {
                Description = "File test",
                Rules = { new Rule { Description = "R1", Expression = "true" } }
            };

            XmlRuleLoader.SaveWorkflowToFile(workflow, path);
            var loaded = XmlRuleLoader.LoadWorkflowFromFile(path);

            loaded.Description.Should().Be("File test");
            loaded.Rules.Should().HaveCount(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ==================== XmlSnapshotSerializer Tests ====================

    [Fact]
    public void XmlSnapshotSerializer_Workflow_RoundTrip_PreservesAllProperties()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Description = "Snapshot workflow",
            Version = new RuleVersion(2, 1, 0),
            IsActive = true,
            ModifiedBy = "test-user"
        };
        workflow.Rules.Add(new Rule
        {
            Id = Guid.NewGuid(),
            Description = "Snapshot rule",
            Expression = "x > 0",
            Action = "result = x",
            Priority = 10,
            IsActive = true,
            Timeout = TimeSpan.FromSeconds(30),
            CacheDuration = TimeSpan.FromMinutes(5),
            Version = new RuleVersion(1, 5, 3),
            ModifiedBy = "rule-user"
        });

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(xml);

        restored.Id.Should().Be(workflow.Id);
        restored.Description.Should().Be("Snapshot workflow");
        restored.Version.Should().Be(new RuleVersion(2, 1, 0));
        restored.IsActive.Should().BeTrue();
        restored.ModifiedBy.Should().Be("test-user");
        restored.Rules.Should().HaveCount(1);

        var rule = restored.Rules[0];
        rule.Id.Should().Be(workflow.Rules[0].Id);
        rule.Description.Should().Be("Snapshot rule");
        rule.Expression.Should().Be("x > 0");
        rule.Action.Should().Be("result = x");
        rule.Priority.Should().Be(10);
        rule.IsActive.Should().BeTrue();
        rule.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        rule.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        rule.Version.Should().Be(new RuleVersion(1, 5, 3));
        rule.ModifiedBy.Should().Be("rule-user");
    }

    [Fact]
    public void XmlSnapshotSerializer_Rule_RoundTrip_PreservesAllProperties()
    {
        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Description = "Standalone snapshot rule",
            Expression = "x > 0",
            Action = "result = x",
            Priority = 7,
            IsActive = true,
            Timeout = TimeSpan.FromSeconds(15),
            CacheDuration = TimeSpan.FromMinutes(2),
            Version = new RuleVersion(3, 2, 1),
            DependsOnRuleId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            ParentRuleId = Guid.NewGuid(),
            ModifiedBy = "author"
        };

        var snapshot = RuleSnapshot.FromRule(rule);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeRule(xml);

        restored.Id.Should().Be(rule.Id);
        restored.Description.Should().Be("Standalone snapshot rule");
        restored.Expression.Should().Be("x > 0");
        restored.Action.Should().Be("result = x");
        restored.Priority.Should().Be(7);
        restored.IsActive.Should().BeTrue();
        restored.Timeout.Should().Be(TimeSpan.FromSeconds(15));
        restored.CacheDuration.Should().Be(TimeSpan.FromMinutes(2));
        restored.Version.Should().Be(new RuleVersion(3, 2, 1));
        restored.DependsOnRuleId.Should().Be(rule.DependsOnRuleId);
        restored.WorkflowId.Should().Be(rule.WorkflowId);
        restored.ParentRuleId.Should().Be(rule.ParentRuleId);
        restored.ModifiedBy.Should().Be("author");
    }

    [Fact]
    public void XmlSnapshotSerializer_Workflow_WithChildRules_RoundTrip()
    {
        var workflow = new Workflow
        {
            Description = "Parent workflow",
            Rules =
            {
                new Rule
                {
                    Description = "Parent",
                    Expression = "true",
                    ChildRules =
                    {
                        new Rule { Description = "Child1", Expression = "x > 0" },
                        new Rule { Description = "Child2", Expression = "false", Priority = 5 }
                    }
                }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(xml);

        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Description.Should().Be("Parent");
        restored.Rules[0].ChildRules.Should().HaveCount(2);
        restored.Rules[0].ChildRules[0].Description.Should().Be("Child1");
        restored.Rules[0].ChildRules[1].Description.Should().Be("Child2");
        restored.Rules[0].ChildRules[1].Priority.Should().Be(5);
    }

    [Fact]
    public void XmlSnapshotSerializer_SaveAndLoad_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var workflow = new Workflow
            {
                Description = "File snapshot test",
                Rules = { new Rule { Description = "R1", Expression = "x > 0" } }
            };

            var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
            var serializer = new XmlSnapshotSerializer();

            serializer.SaveWorkflowToFile(snapshot, path);
            var loaded = serializer.LoadWorkflowFromFile(path);

            loaded.Description.Should().Be("File snapshot test");
            loaded.Rules.Should().HaveCount(1);
            loaded.Rules[0].Description.Should().Be("R1");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void XmlSnapshotSerializer_Rule_SaveAndLoad_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var rule = new Rule
            {
                Description = "Standalone file rule",
                Expression = "y < 100",
                Version = new RuleVersion(1, 2, 3)
            };

            var snapshot = RuleSnapshot.FromRule(rule);
            var serializer = new XmlSnapshotSerializer();

            serializer.SaveRuleToFile(snapshot, path);
            var loaded = serializer.LoadRuleFromFile(path);

            loaded.Description.Should().Be("Standalone file rule");
            loaded.Expression.Should().Be("y < 100");
            loaded.Version.Should().Be(new RuleVersion(1, 2, 3));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void XmlSnapshotSerializer_NullOptionalProperties_RoundTrip()
    {
        var rule = new Rule
        {
            Description = "Minimal rule",
            Expression = "true"
        };

        var snapshot = RuleSnapshot.FromRule(rule);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeRule(xml);

        restored.Description.Should().Be("Minimal rule");
        restored.Expression.Should().Be("true");
        restored.Timeout.Should().BeNull();
        restored.CacheDuration.Should().BeNull();
        restored.DependsOnRuleId.Should().BeNull();
        restored.WorkflowId.Should().BeNull();
        restored.ParentRuleId.Should().BeNull();
        restored.ModifiedBy.Should().BeNull();
        restored.Action.Should().BeEmpty();
    }

    [Fact]
    public void XmlSnapshotSerializer_Workflow_RestoreToLiveModel()
    {
        var original = new Workflow
        {
            Description = "Restore test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0", Priority = 10 }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(original);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var loadedSnapshot = serializer.DeserializeWorkflow(xml);
        var restored = loadedSnapshot.ToWorkflow();

        restored.Description.Should().Be("Restore test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Description.Should().Be("R1");
        restored.Rules[0].Expression.Should().Be("x > 0");
        restored.Rules[0].Priority.Should().Be(10);
    }
}
