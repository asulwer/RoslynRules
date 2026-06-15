using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Execution;
using RoslynRules.Models;
using RoslynRules.Snapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace RoslynRules.Tests.AotCompatibility
{
    /// <summary>
    /// Validates AOT-safe APIs that don't require JIT compilation.
    /// These tests confirm RoslynRules can be referenced from AOT apps
    /// without linker errors for model/validation scenarios.
    /// </summary>
    public class AotCompatibilityTests
    {
        [Fact]
        public void Workflow_ModelCreation()
        {
            var workflow = new Workflow
            {
                Description = "Test Workflow",
                Rules =
                {
                    new Rule
                    {
                        Description = "Age check",
                        Expression = "x >= 18",
                        IsActive = true,
                        Priority = 10
                    },
                    new Rule
                    {
                        Description = "Name check",
                        Expression = "!string.IsNullOrEmpty(name)",
                        IsActive = true,
                        Priority = 5
                    }
                }
            };

            workflow.Rules.Should().HaveCount(2);
            workflow.Rules[0].Priority.Should().Be(10);
        }

        [Fact]
        public void Workflow_Validate_WithoutCompile()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "R1", Expression = "true", IsActive = true },
                    new Rule { Description = "R2", Expression = "false", IsActive = true }
                }
            };

            // Validation does not require compilation
            workflow.Validate();
        }

        [Fact]
        public void Workflow_ValidateDependencyChain()
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

            workflow.Validate();
        }

        [Fact]
        public void Workflow_DetectCycle()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Id = id1, Description = "A", Expression = "true", IsActive = true, DependsOnRuleId = id2 },
                    new Rule { Id = id2, Description = "B", Expression = "true", IsActive = true, DependsOnRuleId = id1 }
                }
            };

            var act = () => workflow.Validate();
            act.Should().Throw<CircularReferenceException>();
        }

        [Fact]
        public void RuleResult_CreateAndAccess()
        {
            var result = new RuleResult(
                Success: true,
                RuleId: Guid.NewGuid(),
                RuleDescription: "Test"
            );

            result.Success.Should().BeTrue();
            result.RuleDescription.Should().Be("Test");
        }

        [Fact]
        public void RuleContext_StoreAndGet()
        {
            var context = new RuleContext();
            var ruleId = Guid.NewGuid();
            var result = new RuleResult(Success: true, RuleId: ruleId, Value: 123);

            context.StoreResult(ruleId, result);
            var retrieved = context.GetResult(ruleId);

            retrieved.Should().NotBeNull();
            retrieved!.Value.Value.Should().Be(123);
        }

        [Fact]
        public void Workflow_GetExecutionOrder_WithoutCompile()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "Low", Expression = "true", IsActive = true, Priority = 0 },
                    new Rule { Description = "High", Expression = "true", IsActive = true, Priority = 10 }
                }
            };

            var method = typeof(Workflow).GetMethod("GetExecutionOrder",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Should().NotBeNull();

            var ordered = method!.Invoke(workflow, null) as List<Rule>;
            ordered.Should().NotBeNull().And.HaveCount(2);
            ordered![0].Description.Should().Be("High");
        }

        [Fact]
        public void Compile_HasRequiresUnreferencedCodeAttribute()
        {
            var compilerType = typeof(RoslynRules.Compiler.ExpressionCompiler);
            var compileMethod = compilerType.GetMethods()
                .FirstOrDefault(m => m.Name == "Compile");

            compileMethod.Should().NotBeNull();

            var attributes = compileMethod!.GetCustomAttributes(
                typeof(System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute), false);
            attributes.Should().NotBeEmpty("Compile should have RequiresUnreferencedCode attribute for AOT safety");
        }

        // ==================== VERSIONING AOT TESTS ====================

        [Fact]
        public void RuleVersion_CreateAndCompare()
        {
            var v1 = new RuleVersion(1, 0, 0);
            var v2 = new RuleVersion(1, 1, 0);
            var v3 = new RuleVersion(2, 0, 0);

            v1.Should().BeLessThan(v2);
            v2.Should().BeLessThan(v3);
            v2.IsCompatibleWith(v1).Should().BeTrue("1.1.0 is backward compatible with 1.0.0 since both have major=1");
            v3.IsCompatibleWith(v1).Should().BeFalse();
        }

        [Fact]
        public void RuleVersion_ParseAndToString()
        {
            var version = RuleVersion.Parse("1.2.3-alpha+build.123");
            version.Major.Should().Be(1);
            version.Minor.Should().Be(2);
            version.Patch.Should().Be(3);
            version.Prerelease.Should().Be("alpha");
            version.BuildMetadata.Should().Be("build.123");
            version.ToString().Should().Be("1.2.3-alpha+build.123");
        }

        [Fact]
        public void Rule_VersionProperties_WorkWithoutCompile()
        {
            var rule = new Rule
            {
                Description = "Test",
                Version = new RuleVersion(2, 0, 0),
                Expression = "true"
            };

            rule.Version.Should().Be(new RuleVersion(2, 0, 0));
            rule.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Workflow_VersionProperties_WorkWithoutCompile()
        {
            var workflow = new Workflow
            {
                Description = "Test",
                Version = new RuleVersion(3, 1, 0),
                Rules =
                {
                    new Rule { Description = "R1", Expression = "true" }
                }
            };

            workflow.Version.Should().Be(new RuleVersion(3, 1, 0));
            workflow.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Workflow_GetRuleVersions_WorkWithoutCompile()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "R1", Expression = "true", Version = new RuleVersion(1, 0, 0) },
                    new Rule { Description = "R2", Expression = "true", Version = new RuleVersion(2, 0, 0) }
                }
            };

            var versions = workflow.GetRuleVersions();
            versions.Should().HaveCount(2);
        }

        [Fact]
        public void RuleVersion_Increment_WorksWithoutCompile()
        {
            var version = new RuleVersion(1, 2, 3);

            version.IncrementMajor().Should().Be(new RuleVersion(2, 0, 0));
            version.IncrementMinor().Should().Be(new RuleVersion(1, 3, 0));
            version.IncrementPatch().Should().Be(new RuleVersion(1, 2, 4));
        }

        // ==================== AOT COMPATIBILITY DETECTION ====================

        [Fact]
        public void AotCompatibility_IsAot_ReturnsFalse_InJitEnvironment()
        {
            // Test runner is JIT — AOT should be false.
            global::RoslynRules.AotCompatibility.IsAot.Should().BeFalse();
        }

        [Fact]
        public void AotCompatibility_ThrowIfAot_DoesNotThrow_InJitEnvironment()
        {
            var act = () => global::RoslynRules.AotCompatibility.ThrowIfAot("TestApi");
            act.Should().NotThrow();
        }

        [Fact]
        public void Workflow_Compile_DoesNotThrow_InJitEnvironment()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "R1", Expression = "true" }
                }
            };

            var act = () => workflow.Compile(new[] { new RuleParameter("x", typeof(int)) });
            act.Should().NotThrow();
        }

        [Fact]
        public void CompiledWorkflow_Compile_DoesNotThrow_InJitEnvironment()
        {
            var workflow = new Workflow
            {
                Rules =
                {
                    new Rule { Description = "R1", Expression = "x > 0" }
                }
            };

            var act = () => CompiledWorkflow.Compile(workflow, new[] { new RuleParameter("x", typeof(int)) });
            act.Should().NotThrow();
        }

        [Fact]
        public void AotCompatibilityException_HasClearMessage()
        {
            var ex = new AotCompatibilityException("Workflow.Compile");

            ex.ApiName.Should().Be("Workflow.Compile");
            ex.Message.Should().Contain("Workflow.Compile");
            ex.Message.Should().Contain("AOT");
            ex.Message.Should().Contain("snapshots");
        }
    }
}
