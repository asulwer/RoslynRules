using FluentAssertions;
using RoslynRules.Json;
using RoslynRules.Models;
using RoslynRules.Snapshots;
using RoslynRules.Xml;
using System;
using System.IO;
using System.Text.Json;
using System.Xml;
using Xunit;

namespace RoslynRules.Tests.Snapshots;

/// <summary>
/// Tests for snapshot creation, serialization, and restoration.
/// Validates JIT-only creation paths and AOT-safe consumption paths.
/// </summary>
public class SnapshotTests
{
    // ==================== SNAPSHOT CREATION (JIT ONLY) ====================

    [Fact]
    public void RuleSnapshot_FromRule_PreservesAllProperties()
    {
        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Description = "Test rule",
            Expression = "x > 0",
            Action = "result = x",
            Priority = 10,
            IsActive = true,
            Timeout = TimeSpan.FromSeconds(30),
            CacheDuration = TimeSpan.FromMinutes(5),
            Version = new RuleVersion(1, 2, 3)
        };

        var snapshot = RuleSnapshot.FromRule(rule);

        snapshot.Id.Should().Be(rule.Id);
        snapshot.Description.Should().Be("Test rule");
        snapshot.Expression.Should().Be("x > 0");
        snapshot.Action.Should().Be("result = x");
        snapshot.Priority.Should().Be(10);
        snapshot.IsActive.Should().BeTrue();
        snapshot.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        snapshot.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        snapshot.Version.Should().Be(new RuleVersion(1, 2, 3));
    }

    [Fact]
    public void RuleSnapshot_FromRule_PreservesChildRules()
    {
        var parent = new Rule
        {
            Description = "Parent",
            Expression = "x > 0"
        };
        parent.ChildRules.Add(new Rule { Description = "Child1", Expression = "x > 0" });
        parent.ChildRules.Add(new Rule { Description = "Child2", Expression = "false" });

        var snapshot = RuleSnapshot.FromRule(parent);

        snapshot.ChildRules.Should().HaveCount(2);
        snapshot.ChildRules[0].Description.Should().Be("Child1");
        snapshot.ChildRules[1].Description.Should().Be("Child2");
    }

    [Fact]
    public void WorkflowSnapshot_FromWorkflow_PreservesAllProperties()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Description = "Test workflow",
            IsActive = true,
            Version = new RuleVersion(2, 0, 0)
        };
        workflow.Rules.Add(new Rule { Description = "R1", Expression = "x > 0" });
        workflow.Rules.Add(new Rule { Description = "R2", Expression = "false" });

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);

        snapshot.Id.Should().Be(workflow.Id);
        snapshot.Description.Should().Be("Test workflow");
        snapshot.IsActive.Should().BeTrue();
        snapshot.Version.Should().Be(new RuleVersion(2, 0, 0));
        snapshot.Rules.Should().HaveCount(2);
    }

    // ==================== SNAPSHOT RESTORATION (AOT SAFE) ====================

    [Fact]
    public void RuleSnapshot_ToRule_RestoresAllProperties()
    {
        var original = new Rule
        {
            Id = Guid.NewGuid(),
            Description = "Test rule",
            Expression = "x > 0",
            Action = "result = x",
            Priority = 10,
            IsActive = true,
            Timeout = TimeSpan.FromSeconds(30),
            CacheDuration = TimeSpan.FromMinutes(5),
            Version = new RuleVersion(1, 2, 3)
        };

        var snapshot = RuleSnapshot.FromRule(original);
        var restored = snapshot.ToRule();

        restored.Id.Should().Be(original.Id);
        restored.Description.Should().Be("Test rule");
        restored.Expression.Should().Be("x > 0");
        restored.Action.Should().Be("result = x");
        restored.Priority.Should().Be(10);
        restored.IsActive.Should().BeTrue();
        restored.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        restored.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
        restored.Version.Should().Be(new RuleVersion(1, 2, 3));
    }

    [Fact]
    public void WorkflowSnapshot_ToWorkflow_RestoresAllProperties()
    {
        var original = new Workflow
        {
            Id = Guid.NewGuid(),
            Description = "Test workflow",
            IsActive = true,
            Version = new RuleVersion(2, 0, 0)
        };
        original.Rules.Add(new Rule { Description = "R1", Expression = "x > 0" });

        var snapshot = WorkflowSnapshot.FromWorkflow(original);
        var restored = snapshot.ToWorkflow();

        restored.Id.Should().Be(original.Id);
        restored.Description.Should().Be("Test workflow");
        restored.IsActive.Should().BeTrue();
        restored.Version.Should().Be(new RuleVersion(2, 0, 0));
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Description.Should().Be("R1");
    }

    [Fact]
    public void RuleSnapshot_ToRule_RestoresChildRules()
    {
        var original = new Rule
        {
            Description = "Parent",
            Expression = "x > 0"
        };
        original.ChildRules.Add(new Rule { Description = "Child", Expression = "x > 0" });

        var snapshot = RuleSnapshot.FromRule(original);
        var restored = snapshot.ToRule();

        restored.ChildRules.Should().HaveCount(1);
        restored.ChildRules[0].Description.Should().Be("Child");
    }

    // ==================== JSON SERIALIZER ====================

    [Fact]
    public void JsonSnapshotSerializer_Workflow_RoundTrip()
    {
        var workflow = new Workflow
        {
            Description = "JSON Snapshot Test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0", Priority = 5 }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new JsonSnapshotSerializer();

        var json = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(json);

        restored.Description.Should().Be("JSON Snapshot Test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Description.Should().Be("R1");
        restored.Rules[0].Priority.Should().Be(5);
    }

    [Fact]
    public void JsonSnapshotSerializer_Rule_RoundTrip()
    {
        var rule = new Rule
        {
            Description = "Rule Snapshot",
            Expression = "x > 0",
            Version = new RuleVersion(1, 2, 3)
        };

        var snapshot = RuleSnapshot.FromRule(rule);
        var serializer = new JsonSnapshotSerializer();

        var json = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeRule(json);

        restored.Description.Should().Be("Rule Snapshot");
        restored.Expression.Should().Be("x > 0");
        restored.Version.Should().Be(new RuleVersion(1, 2, 3));
    }

    // ==================== XML SERIALIZER ====================

    [Fact]
    public void XmlSnapshotSerializer_Workflow_RoundTrip()
    {
        var workflow = new Workflow
        {
            Description = "XML Snapshot Test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0", Priority = 5 }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(xml);

        restored.Description.Should().Be("XML Snapshot Test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Description.Should().Be("R1");
        restored.Rules[0].Priority.Should().Be(5);
    }

    [Fact]
    public void XmlSnapshotSerializer_Rule_RoundTrip()
    {
        var rule = new Rule
        {
            Description = "Rule Snapshot",
            Expression = "x > 0",
            Version = new RuleVersion(1, 2, 3)
        };

        var snapshot = RuleSnapshot.FromRule(rule);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeRule(xml);

        restored.Description.Should().Be("Rule Snapshot");
        restored.Expression.Should().Be("x > 0");
        restored.Version.Should().Be(new RuleVersion(1, 2, 3));
    }

    // ==================== SNAPSHOT MANAGER ====================

    [Fact]
    public void SnapshotManager_CreateSnapshot_FromCompiledWorkflow()
    {
        var workflow = new Workflow
        {
            Description = "Snapshot Manager Test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0" }
            }
        };

        // Compile with no parameters (expression is just "true")
        var param = new RuleParameter("x", typeof(int), 1);
        var compiled = CompiledWorkflow.Compile(workflow, new[] { param });
        var snapshot = SnapshotManager.CreateSnapshot(compiled);

        snapshot.Description.Should().Be("Snapshot Manager Test");
        snapshot.Rules.Should().HaveCount(1);
    }

    [Fact]
    public void SnapshotManager_RestoreWorkflow_FromSnapshot()
    {
        var original = new Workflow
        {
            Description = "Restore Test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0", Priority = 10 }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(original);
        var restored = SnapshotManager.RestoreWorkflow(snapshot);

        restored.Description.Should().Be("Restore Test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Priority.Should().Be(10);
    }

    [Fact]
    public void SnapshotManager_SaveAndLoad_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var workflow = new Workflow
            {
                Description = "File Snapshot Test",
                Rules =
                {
                    new Rule { Description = "R1", Expression = "x > 0" }
                }
            };

            var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
            var serializer = new JsonSnapshotSerializer();

            SnapshotManager.SaveSnapshot(snapshot, serializer, path);
            var loaded = SnapshotManager.LoadSnapshot(serializer, path);

            loaded.Description.Should().Be("File Snapshot Test");
            loaded.Rules.Should().HaveCount(1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ==================== COMPLEX SCENARIOS ====================

    [Fact]
    public void JsonSnapshotSerializer_ComplexNestedRules_RoundTrip()
    {
        var workflow = new Workflow
        {
            Description = "Complex Workflow",
            Rules =
            {
                new Rule
                {
                    Description = "Parent Rule",
                    Expression = "x > 0",
                    Priority = 10,
                    Action = "result = x * 2",
                    Timeout = TimeSpan.FromSeconds(30),
                    CacheDuration = TimeSpan.FromMinutes(5),
                    ChildRules =
                    {
                        new Rule
                        {
                            Description = "Child 1",
                            Expression = "x > 10",
                            Priority = 5,
                            DependsOnRuleId = Guid.NewGuid()
                        },
                        new Rule
                        {
                            Description = "Child 2",
                            Expression = "x < 100",
                            Priority = 3,
                            Action = "result = x / 2",
                            ChildRules =
                            {
                                new Rule
                                {
                                    Description = "Grandchild",
                                    Expression = "x == 50",
                                    Priority = 1
                                }
                            }
                        }
                    }
                }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new JsonSnapshotSerializer();

        var json = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(json);

        restored.Description.Should().Be("Complex Workflow");
        restored.Rules.Should().HaveCount(1);
        
        var parent = restored.Rules[0];
        parent.Description.Should().Be("Parent Rule");
        parent.ChildRules.Should().HaveCount(2);
        
        var child2 = parent.ChildRules[1];
        child2.ChildRules.Should().HaveCount(1);
        child2.ChildRules[0].Description.Should().Be("Grandchild");
    }

    [Fact]
    public void XmlSnapshotSerializer_AllOptionalFields_RoundTrip()
    {
        var workflow = new Workflow
        {
            Id = Guid.NewGuid(),
            Description = "All Fields Test",
            ModifiedBy = "test-user",
            Version = new RuleVersion(1, 2, 3, "beta.1", "build.456"),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ModifiedAt = DateTime.UtcNow,
            IsActive = true,
            Rules =
            {
                new Rule
                {
                    Id = Guid.NewGuid(),
                    Description = "Full Rule",
                    DescriptionKey = "rule.full.description",
                    Expression = "customer.Age >= 18",
                    Action = "customer.IsAdult = true",
                    IsActive = true,
                    Priority = 100,
                    Timeout = TimeSpan.FromSeconds(60),
                    CacheDuration = TimeSpan.FromMinutes(10),
                    WorkflowId = Guid.NewGuid(),
                    ParentRuleId = Guid.NewGuid(),
                    DependsOnRuleId = Guid.NewGuid(),
                    ModifiedBy = "rule-author",
                    Version = new RuleVersion(2, 0, 0),
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    ModifiedAt = DateTime.UtcNow.AddHours(-1)
                }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new XmlSnapshotSerializer();

        var xml = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(xml);

        restored.Id.Should().Be(workflow.Id);
        restored.Description.Should().Be("All Fields Test");
        restored.ModifiedBy.Should().Be("test-user");
        restored.Version.Should().Be(new RuleVersion(1, 2, 3, "beta.1", "build.456"));
        
        var rule = restored.Rules[0];
        rule.DescriptionKey.Should().Be("rule.full.description");
        rule.Action.Should().Be("customer.IsAdult = true");
        rule.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        rule.CacheDuration.Should().Be(TimeSpan.FromMinutes(10));
        rule.WorkflowId.Should().Be(workflow.Rules[0].WorkflowId);
        rule.ParentRuleId.Should().Be(workflow.Rules[0].ParentRuleId);
        rule.DependsOnRuleId.Should().Be(workflow.Rules[0].DependsOnRuleId);
        rule.ModifiedBy.Should().Be("rule-author");
    }

    [Fact]
    public void JsonSnapshotSerializer_MultipleRulesWithDependencies_RoundTrip()
    {
        var rule1Id = Guid.NewGuid();
        var rule2Id = Guid.NewGuid();
        
        var workflow = new Workflow
        {
            Description = "Dependency Chain",
            Rules =
            {
                new Rule
                {
                    Id = rule1Id,
                    Description = "First Rule",
                    Expression = "true",
                    Priority = 10
                },
                new Rule
                {
                    Id = rule2Id,
                    Description = "Second Rule",
                    Expression = "result == true",
                    Priority = 5,
                    DependsOnRuleId = rule1Id
                }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
        var serializer = new JsonSnapshotSerializer();

        var json = serializer.Serialize(snapshot);
        var restored = serializer.DeserializeWorkflow(json);

        restored.Rules.Should().HaveCount(2);
        restored.Rules[1].DependsOnRuleId.Should().Be(rule1Id);
    }

    // ==================== ERROR HANDLING ====================

    [Fact]
    public void JsonSnapshotSerializer_InvalidJson_ThrowsJsonException()
    {
        var serializer = new JsonSnapshotSerializer();
        var invalidJson = "{ not valid json }";

        Action act = () => serializer.DeserializeWorkflow(invalidJson);
        
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void JsonSnapshotSerializer_EmptyString_ThrowsJsonException()
    {
        var serializer = new JsonSnapshotSerializer();
        
        Action act = () => serializer.DeserializeWorkflow(string.Empty);
        
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void JsonSnapshotSerializer_NullString_ThrowsArgumentNullException()
    {
        var serializer = new JsonSnapshotSerializer();
        
        Action act = () => serializer.DeserializeWorkflow(null!);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void XmlSnapshotSerializer_InvalidXml_ThrowsXmlException()
    {
        var serializer = new XmlSnapshotSerializer();
        var invalidXml = "<not valid xml";

        Action act = () => serializer.DeserializeWorkflow(invalidXml);
        
        act.Should().Throw<System.Xml.XmlException>();
    }

    [Fact]
    public void XmlSnapshotSerializer_EmptyString_ThrowsXmlException()
    {
        var serializer = new XmlSnapshotSerializer();
        
        Action act = () => serializer.DeserializeWorkflow(string.Empty);
        
        act.Should().Throw<System.Xml.XmlException>();
    }

    [Fact]
    public void XmlSnapshotSerializer_WrongRootElement_ParsesWithDefaults()
    {
        // Wrong root element will still parse, but with default values
        var serializer = new XmlSnapshotSerializer();
        var wrongXml = "<WrongRoot><Description>test</Description></WrongRoot>";

        // This should NOT throw - it will parse with defaults
        var result = serializer.DeserializeWorkflow(wrongXml);
        
        // The result will have default values since the XML structure doesn't match
        result.Should().NotBeNull();
    }

    // ==================== SERIALIZER EXTENSIONS ====================

    [Fact]
    public void JsonSnapshotSerializer_SaveAndLoadWorkflow_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var workflow = new Workflow
            {
                Description = "Extension Method Test",
                Rules =
                {
                    new Rule { Description = "R1", Expression = "x > 0", Priority = 5 }
                }
            };

            var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
            var serializer = new JsonSnapshotSerializer();

            // Use extension method
            serializer.SaveWorkflowToFile(snapshot, path);
            var loaded = serializer.LoadWorkflowFromFile(path);

            loaded.Description.Should().Be("Extension Method Test");
            loaded.Rules[0].Priority.Should().Be(5);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void XmlSnapshotSerializer_SaveAndLoadWorkflow_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var workflow = new Workflow
            {
                Description = "XML Extension Method Test",
                Rules =
                {
                    new Rule { Description = "R1", Expression = "x > 0", Priority = 10 }
                }
            };

            var snapshot = WorkflowSnapshot.FromWorkflow(workflow);
            var serializer = new XmlSnapshotSerializer();

            // Use extension method
            serializer.SaveWorkflowToFile(snapshot, path);
            var loaded = serializer.LoadWorkflowFromFile(path);

            loaded.Description.Should().Be("XML Extension Method Test");
            loaded.Rules[0].Priority.Should().Be(10);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void JsonSnapshotSerializer_SaveAndLoadRule_FileRoundTrip()
    {
        var path = Path.GetTempFileName();
        try
        {
            var rule = new Rule
            {
                Description = "Rule Extension Test",
                Expression = "x > 0",
                Priority = 15
            };

            var snapshot = RuleSnapshot.FromRule(rule);
            var serializer = new JsonSnapshotSerializer();

            // Use extension method for Rule
            serializer.SaveRuleToFile(snapshot, path);
            var loaded = serializer.LoadRuleFromFile(path);

            loaded.Description.Should().Be("Rule Extension Test");
            loaded.Priority.Should().Be(15);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ==================== INTEGRATION: JIT COMPILE -> SNAPSHOT -> AOT RESTORE ====================

    [Fact]
    public void Integration_Compile_Snapshot_Serialize_Deserialize_Restore()
    {
        // Step 1: Create and compile workflow (JIT)
        var workflow = new Workflow
        {
            Description = "Integration Test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0", Priority = 5 }
            }
        };

        var param = new RuleParameter("x", typeof(int), 1);
        var compiled = CompiledWorkflow.Compile(workflow, new[] { param });

        // Step 2: Create snapshot (JIT - uses reflection)
        var snapshot = compiled.ToSnapshot();

        // Step 3: Serialize to JSON (AOT-safe)
        var serializer = new JsonSnapshotSerializer();
        var json = serializer.Serialize(snapshot);

        // Step 4: Deserialize (AOT-safe)
        var loadedSnapshot = serializer.DeserializeWorkflow(json);

        // Step 5: Restore workflow (AOT-safe)
        var restored = SnapshotManager.RestoreWorkflow(loadedSnapshot);

        restored.Description.Should().Be("Integration Test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Description.Should().Be("R1");
        restored.Rules[0].Priority.Should().Be(5);
    }

    [Fact]
    public void Integration_Xml_Serialize_Deserialize_Restore()
    {
        // Step 1: Create workflow
        var workflow = new Workflow
        {
            Description = "XML Integration Test",
            Rules =
            {
                new Rule { Description = "R1", Expression = "x > 0", Priority = 7 }
            }
        };

        var snapshot = WorkflowSnapshot.FromWorkflow(workflow);

        // Step 2: Serialize to XML (AOT-safe)
        var serializer = new XmlSnapshotSerializer();
        var xml = serializer.Serialize(snapshot);

        // Step 3: Deserialize (AOT-safe)
        var loadedSnapshot = serializer.DeserializeWorkflow(xml);

        // Step 4: Restore workflow (AOT-safe)
        var restored = SnapshotManager.RestoreWorkflow(loadedSnapshot);

        restored.Description.Should().Be("XML Integration Test");
        restored.Rules.Should().HaveCount(1);
        restored.Rules[0].Priority.Should().Be(7);
    }
}
