using FluentAssertions;
using RoslynRules.Json;
using RoslynRules.Models;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Integration
{
    /// <summary>
    /// Tests for JSON serialization and deserialization of Rules and Workflows.
    /// </summary>
    public class JsonRuleLoaderTests : IDisposable
    {
        private readonly string _tempFile;

        public JsonRuleLoaderTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"test-workflow-{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        [Fact]
        public void SerializeDeserialize_SimpleWorkflow_RestoresCorrectly()
        {
            var original = new Workflow
            {
                Description = "Test workflow",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Description = "Adult check",
                        Expression = "customer.Age >= 18",
                        IsActive = true
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Description.Should().Be(original.Description);
            restored.IsActive.Should().BeTrue();
            restored.Rules.Should().HaveCount(1);
            restored.Rules[0].Description.Should().Be("Adult check");
            restored.Rules[0].Expression.Should().Be("customer.Age >= 18");
            restored.Rules[0].IsActive.Should().BeTrue();
        }

        [Fact]
        public void SerializeDeserialize_WorkflowWithChildren_RestoresHierarchy()
        {
            var original = new Workflow
            {
                Description = "Parent-child workflow",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Description = "Parent",
                        Expression = "true",
                        IsActive = true,
                        ChildRules = new List<Rule>
                        {
                            new Rule
                            {
                                Description = "Child 1",
                                Expression = "customer.Age > 0",
                                IsActive = true
                            },
                            new Rule
                            {
                                Description = "Child 2",
                                Expression = "customer.Name != null",
                                IsActive = true
                            }
                        }
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules.Should().HaveCount(1);
            var parent = restored.Rules[0];
            parent.Description.Should().Be("Parent");
            parent.ChildRules.Should().HaveCount(2);
            parent.ChildRules[0].Description.Should().Be("Child 1");
            parent.ChildRules[1].Description.Should().Be("Child 2");
        }

        [Fact]
        public void SerializeDeserialize_PreservesIds()
        {
            var ruleId = Guid.NewGuid();
            var original = new Workflow
            {
                Rules = new List<Rule>
                {
                    new Rule(ruleId)
                    {
                        Description = "Rule with ID"
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules[0].Id.Should().Be(ruleId);
        }

        [Fact]
        public void SaveToFile_LoadFromFile_RoundTrip()
        {
            var original = new Workflow
            {
                Description = "File test",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Description = "File rule",
                        Expression = "1 == 1",
                        Action = "customer.Processed = true"
                    }
                }
            };

            JsonRuleLoader.SaveWorkflowToFile(original, _tempFile);
            File.Exists(_tempFile).Should().BeTrue();

            var restored = JsonRuleLoader.LoadWorkflowFromFile(_tempFile);
            restored.Description.Should().Be("File test");
            restored.Rules[0].Expression.Should().Be("1 == 1");
            restored.Rules[0].Action.Should().Be("customer.Processed = true");
        }

        [Fact]
        public void Serialize_InactiveRules_PreservesState()
        {
            var original = new Workflow
            {
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Description = "Active rule",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Inactive rule",
                        IsActive = false
                    }
                }
            };

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules.Should().HaveCount(2);
            restored.Rules[0].IsActive.Should().BeTrue();
            restored.Rules[1].IsActive.Should().BeFalse();
        }

        [Fact]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            var act = () => JsonRuleLoader.DeserializeWorkflow("not valid json");
            act.Should().Throw<System.Text.Json.JsonException>();
        }

        [Fact]
        public void Serialize_EmptyWorkflow_RestoresEmpty()
        {
            var original = new Workflow
            {
                Description = "Empty"
            };

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Description.Should().Be("Empty");
            restored.Rules.Should().BeEmpty();
        }

        [Fact]
        public void SerializeDeserialize_MultipleTopLevelRules_RestoresAll()
        {
            var original = new Workflow
            {
                Rules = new List<Rule>
                {
                    new Rule { Description = "Rule 1", Expression = "true" },
                    new Rule { Description = "Rule 2", Expression = "false" },
                    new Rule { Description = "Rule 3", Expression = "1 > 0" }
                }
            };

            var json = JsonRuleLoader.Serialize(original);
            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules.Should().HaveCount(3);
            restored.Rules.Select(r => r.Description).Should().Equal("Rule 1", "Rule 2", "Rule 3");
        }

        [Fact]
        public void Deserialize_ComplexNestedHierarchy_RestoresStructure()
        {
            var json = @"{
                ""description"": ""Complex workflow"",
                ""isActive"": true,
                ""rules"": [
                    {
                        ""description"": ""Level 1 Parent"",
                        ""expression"": ""true"",
                        ""childRules"": [
                            {
                                ""description"": ""Level 2 Child"",
                                ""expression"": ""true"",
                                ""childRules"": [
                                    {
                                        ""description"": ""Level 3 Grandchild"",
                                        ""expression"": ""customer.Age > 18""
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }";

            var restored = JsonRuleLoader.DeserializeWorkflow(json);

            restored.Rules[0].Description.Should().Be("Level 1 Parent");
            restored.Rules[0].ChildRules[0].Description.Should().Be("Level 2 Child");
            restored.Rules[0].ChildRules[0].ChildRules[0].Description.Should().Be("Level 3 Grandchild");
        }
    }
}