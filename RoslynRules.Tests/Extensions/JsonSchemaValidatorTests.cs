using FluentAssertions;
using RoslynRules.Json;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Extensions
{
    /// <summary>
    /// Tests for JsonSchemaValidator structural validation.
    /// </summary>
    public class JsonSchemaValidatorTests
    {
        // ==================== Workflow Validation ====================

        [Fact]
        public void ValidateWorkflow_ValidJson_ReturnsNoErrors()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test workflow"",
                ""version"": ""1.0.0"",
                ""isActive"": true,
                ""rules"": [
                    {
                        ""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"",
                        ""description"": ""R1"",
                        ""expression"": ""x > 0"",
                        ""isActive"": true,
                        ""priority"": 0
                    }
                ]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateWorkflow_MissingId_ReturnsError()
        {
            var json = @"{
                ""description"": ""Test"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("Missing required field: 'id'"));
        }

        [Fact]
        public void ValidateWorkflow_InvalidGuid_ReturnsError()
        {
            var json = @"{
                ""id"": ""not-a-guid"",
                ""description"": ""Test"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("'id' must be a valid GUID"));
        }

        [Fact]
        public void ValidateWorkflow_MissingDescription_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("Missing required field: 'description'"));
        }

        [Fact]
        public void ValidateWorkflow_MissingRules_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test""
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("Missing required field: 'rules'"));
        }

        [Fact]
        public void ValidateWorkflow_EmptyRulesArray_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""rules"": []
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("'rules' must contain at least one rule"));
        }

        [Fact]
        public void ValidateWorkflow_InvalidVersion_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""version"": ""abc"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("'version' must be a valid SemVer"));
        }

        [Fact]
        public void ValidateWorkflow_InvalidIsActive_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""isActive"": ""yes"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("'isActive' must be a boolean"));
        }

        [Fact]
        public void ValidateWorkflow_InvalidJson_ReturnsError()
        {
            var json = "{ broken json";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("Invalid JSON"));
        }

        [Fact]
        public void ValidateWorkflow_ArrayRoot_ReturnsError()
        {
            var json = "[]";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().Contain(e => e.Contains("Root element must be a JSON object"));
        }

        // ==================== Rule Validation ====================

        [Fact]
        public void ValidateRule_ValidJson_ReturnsNoErrors()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test rule"",
                ""expression"": ""x > 0"",
                ""version"": ""1.2.3"",
                ""isActive"": true,
                ""priority"": 10,
                ""action"": ""result = x"",
                ""cacheDuration"": 300,
                ""timeout"": 30,
                ""dependsOnRuleId"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"",
                ""descriptionKey"": ""rule.test""
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRule_MissingId_ReturnsError()
        {
            var json = @"{
                ""description"": ""Test"",
                ""expression"": ""true""
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().Contain(e => e.Contains("Missing required field: 'id'"));
        }

        [Fact]
        public void ValidateRule_MissingExpression_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test""
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().Contain(e => e.Contains("Missing required field: 'expression'"));
        }

        [Fact]
        public void ValidateRule_InvalidPriority_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""expression"": ""true"",
                ""priority"": ""high""
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().Contain(e => e.Contains("'priority' must be an integer"));
        }

        [Fact]
        public void ValidateRule_InvalidCacheDuration_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""expression"": ""true"",
                ""cacheDuration"": ""5m""
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().Contain(e => e.Contains("'cacheDuration' must be a number"));
        }

        [Fact]
        public void ValidateRule_InvalidDependsOnRuleId_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""expression"": ""true"",
                ""dependsOnRuleId"": ""bad-guid""
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().Contain(e => e.Contains("'dependsOnRuleId' must be a valid GUID"));
        }

        [Fact]
        public void ValidateRule_NullOptionalFields_AreValid()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""expression"": ""true"",
                ""dependsOnRuleId"": null,
                ""parentRuleId"": null,
                ""workflowId"": null,
                ""cacheDuration"": null,
                ""timeout"": null
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRule_WithChildRules_ValidatesChildren()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Parent"",
                ""expression"": ""true"",
                ""childRules"": [
                    {
                        ""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"",
                        ""description"": ""Child"",
                        ""expression"": ""false""
                    }
                ]
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRule_ChildRuleMissingExpression_ReturnsError()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Parent"",
                ""expression"": ""true"",
                ""childRules"": [
                    {
                        ""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"",
                        ""description"": ""Child""
                    }
                ]
            }";

            var errors = JsonSchemaValidator.ValidateRule(json);
            errors.Should().Contain(e => e.Contains("childRules[0].expression' is required"));
        }

        // ==================== Prerelease Version Validation ====================

        [Fact]
        public void ValidateWorkflow_PrereleaseVersion_IsValid()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""version"": ""1.0.0-beta.2"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateWorkflow_BuildMetadataVersion_IsValid()
        {
            var json = @"{
                ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                ""description"": ""Test"",
                ""version"": ""1.0.0+build.123"",
                ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
            }";

            var errors = JsonSchemaValidator.ValidateWorkflow(json);
            errors.Should().BeEmpty();
        }

        // ==================== Integration with JsonRuleLoader ====================

        [Fact]
        public void LoadWorkflowFromFile_WithValidation_InvalidFile_Throws()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, @"{
                    ""id"": ""bad-guid"",
                    ""description"": ""Test"",
                    ""rules"": []
                }");

                var act = () => JsonRuleLoader.LoadWorkflowFromFile(path, validateSchema: true);
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
                File.WriteAllText(path, @"{
                    ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                    ""description"": ""Test workflow"",
                    ""version"": ""1.0.0"",
                    ""rules"": [
                        {
                            ""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"",
                            ""description"": ""R1"",
                            ""expression"": ""true"",
                            ""isActive"": true,
                            ""priority"": 0
                        }
                    ]
                }");

                var workflow = JsonRuleLoader.LoadWorkflowFromFile(path, validateSchema: true);
                workflow.Description.Should().Be("Test workflow");
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
                // Missing required fields, but validation is off — deserializer may handle it
                File.WriteAllText(path, @"{
                    ""id"": ""550e8400-e29b-41d4-a716-446655440000"",
                    ""description"": ""Test"",
                    ""rules"": [{""id"": ""6ba7b810-9dad-11d1-80b4-00c04fd430c8"", ""description"": ""R1"", ""expression"": ""true""}]
                }");

                var workflow = JsonRuleLoader.LoadWorkflowFromFile(path, validateSchema: false);
                workflow.Should().NotBeNull();
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
