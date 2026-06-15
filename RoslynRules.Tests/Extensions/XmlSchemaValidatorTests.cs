using FluentAssertions;
using RoslynRules.Models;
using RoslynRules.Xml;
using System;
using System.IO;
using Xunit;

namespace RoslynRules.Tests.Extensions
{
    /// <summary>
    /// Tests for XmlSchemaValidator XSD validation.
    /// </summary>
    public class XmlSchemaValidatorTests
    {
        // ==================== Workflow Validation ====================

        [Fact]
        public void ValidateWorkflow_ValidXml_ReturnsNoErrors()
        {
            var workflow = new Workflow
            {
                Description = "Test workflow",
                Rules =
                {
                    new Rule
                    {
                        Description = "R1",
                        Expression = "x > 0",
                        IsActive = true,
                        Priority = 0
                    }
                }
            };

            var xml = XmlRuleLoader.Serialize(workflow);
            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateWorkflow_FullProperties_ReturnsNoErrors()
        {
            var workflow = new Workflow
            {
                Description = "Full workflow",
                IsActive = true,
                ModifiedBy = "admin",
                Rules =
                {
                    new Rule
                    {
                        Description = "Full rule",
                        DescriptionKey = "rule.full",
                        Expression = "x > 0",
                        Action = "result = x",
                        IsActive = true,
                        Priority = 10,
                        Timeout = TimeSpan.FromSeconds(30),
                        CacheDuration = TimeSpan.FromMinutes(5),
                        DependsOnRuleId = Guid.NewGuid(),
                        ParentRuleId = Guid.NewGuid(),
                        WorkflowId = Guid.NewGuid(),
                        ModifiedBy = "author",
                        ChildRules =
                        {
                            new Rule { Description = "Child", Expression = "false", Priority = 5 }
                        }
                    }
                }
            };

            var xml = XmlRuleLoader.Serialize(workflow);
            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateWorkflow_MissingRequiredAttribute_ReturnsError()
        {
            var xml = "<Workflow Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\">\n  <Description>Test</Description>\n  <Rules><Rule Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\" Priority=\"0\">\n    <Description>R1</Description><Expression>true</Expression>\n  </Rule></Rules>\n</Workflow>";

            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().Contain(e => e.Contains("Id") && e.Contains("required"));
        }

        [Fact]
        public void ValidateWorkflow_InvalidGuid_ReturnsError()
        {
            var xml = "<Workflow Id=\"not-a-guid\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\">\n  <Description>Test</Description>\n  <Rules><Rule Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\" Priority=\"0\">\n    <Description>R1</Description><Expression>true</Expression>\n  </Rule></Rules>\n</Workflow>";

            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().Contain(e => e.Contains("Id") && e.Contains("invalid"));
        }

        [Fact]
        public void ValidateWorkflow_InvalidVersion_ReturnsError()
        {
            var xml = "<Workflow Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"abc\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\">\n  <Description>Test</Description>\n  <Rules><Rule Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\" Priority=\"0\">\n    <Description>R1</Description><Expression>true</Expression>\n  </Rule></Rules>\n</Workflow>";

            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().Contain(e => e.Contains("Version") && e.Contains("invalid"));
        }

        [Fact]
        public void ValidateWorkflow_EmptyRules_ReturnsError()
        {
            var xml = "<Workflow Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\">\n  <Description>Test</Description>\n  <Rules></Rules>\n</Workflow>";

            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().Contain(e => e.Contains("Rule") && e.Contains("incomplete"));
        }

        [Fact]
        public void ValidateWorkflow_PrereleaseVersion_IsValid()
        {
            var workflow = new Workflow
            {
                Version = new RuleVersion(1, 0, 0, "beta.2"),
                Rules = { new Rule { Description = "R1", Expression = "true" } }
            };

            var xml = XmlRuleLoader.Serialize(workflow);
            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateWorkflow_BuildMetadataVersion_IsValid()
        {
            var workflow = new Workflow
            {
                Version = new RuleVersion(1, 0, 0, buildMetadata: "build.123"),
                Rules = { new Rule { Description = "R1", Expression = "true" } }
            };

            var xml = XmlRuleLoader.Serialize(workflow);
            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateWorkflow_BrokenXml_ReturnsError()
        {
            var xml = "<Workflow><broken";

            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            errors.Should().Contain(e => e.Contains("XML parse error"));
        }

        // ==================== Rule Validation ====================

        [Fact]
        public void ValidateRule_ValidXml_ReturnsNoErrors()
        {
            var rule = new Rule
            {
                Description = "Test rule",
                Expression = "x > 0",
                IsActive = true,
                Priority = 10
            };

            var xml = XmlRuleLoader.Serialize(rule);
            var errors = XmlSchemaValidator.ValidateRule(xml);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRule_MissingRequiredAttribute_ReturnsError()
        {
            var xml = "<Rule Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\" Priority=\"0\">\n  <Description>R1</Description><Expression>true</Expression>\n</Rule>";

            var errors = XmlSchemaValidator.ValidateRule(xml);
            errors.Should().Contain(e => e.Contains("Id") && e.Contains("required"));
        }

        [Fact]
        public void ValidateRule_InvalidPriority_ReturnsError()
        {
            var xml = "<Rule Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\" Priority=\"high\">\n  <Description>R1</Description><Expression>true</Expression>\n</Rule>";

            var errors = XmlSchemaValidator.ValidateRule(xml);
            errors.Should().Contain(e => e.Contains("Priority") && e.Contains("invalid"));
        }

        // ==================== Integration with XmlRuleLoader ====================

        [Fact]
        public void LoadWorkflowFromFile_WithValidation_InvalidFile_Throws()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "<Workflow Id=\"bad-guid\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\">\n  <Description>Test</Description>\n  <Rules></Rules>\n</Workflow>");

                var act = () => XmlRuleLoader.LoadWorkflowFromFile(path, validateSchema: true);
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("*schema validation failed*");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LoadWorkflowFromFile_WithValidation_ValidFile_Succeeds()
        {
            var path = Path.GetTempFileName();
            try
            {
                var workflow = new Workflow
                {
                    Description = "Valid workflow",
                    Rules = { new Rule { Description = "R1", Expression = "true" } }
                };

                XmlRuleLoader.SaveWorkflowToFile(workflow, path);
                var loaded = XmlRuleLoader.LoadWorkflowFromFile(path, validateSchema: true);
                loaded.Description.Should().Be("Valid workflow");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LoadRuleFromFile_WithValidation_ValidFile_Succeeds()
        {
            var path = Path.GetTempFileName();
            try
            {
                var rule = new Rule
                {
                    Description = "Valid rule",
                    Expression = "x > 0",
                    Priority = 5
                };

                XmlRuleLoader.SaveRuleToFile(rule, path);
                var loaded = XmlRuleLoader.LoadRuleFromFile(path, validateSchema: true);
                loaded.Description.Should().Be("Valid rule");
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LoadWorkflowFromFile_WithoutValidation_InvalidFile_Succeeds()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "<Workflow Id=\"550e8400-e29b-41d4-a716-446655440000\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\">\n  <Description>Test</Description>\n  <Rules>\n    <Rule Id=\"550e8400-e29b-41d4-a716-446655440001\" Version=\"1.0.0\" CreatedAt=\"2026-01-01T00:00:00Z\" ModifiedAt=\"2026-01-01T00:00:00Z\" IsActive=\"true\" Priority=\"0\">\n      <Description>R1</Description><Expression>true</Expression>\n    </Rule>\n  </Rules>\n</Workflow>");

                var workflow = XmlRuleLoader.LoadWorkflowFromFile(path, validateSchema: false);
                workflow.Should().NotBeNull();
            }
            finally
            {
                File.Delete(path);
            }
        }

        // ==================== Schema Availability ====================

        [Fact]
        public void WorkflowXsd_IsNotNullOrEmpty()
        {
            XmlSchemaValidator.WorkflowXsd.Should().NotBeNullOrEmpty();
            XmlSchemaValidator.WorkflowXsd.Should().Contain("WorkflowType");
            XmlSchemaValidator.WorkflowXsd.Should().Contain("RuleType");
        }

        [Fact]
        public void RuleXsd_IsNotNullOrEmpty()
        {
            XmlSchemaValidator.RuleXsd.Should().NotBeNullOrEmpty();
            XmlSchemaValidator.RuleXsd.Should().Contain("RuleType");
        }
    }
}
