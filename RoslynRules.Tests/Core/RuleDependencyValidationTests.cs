using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace RoslynRules.Tests.Core
{
    /// <summary>
    /// Tests for Rule dependency validation (DependsOnRuleId checks).
    /// </summary>
    public class RuleDependencyValidationTests
    {
        [Fact]
        public void Validate_WithValidDependency_NoException()
        {
            var depId = Guid.NewGuid();
            var rule = new Rule
            {
                Description = "Dependent rule",
                Expression = "true",
                DependsOnRuleId = depId
            };

            var availableIds = new List<Guid> { depId };
            var act = () => rule.Validate(availableIds);
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_WithMissingDependency_ThrowsRuleValidationException()
        {
            var missingId = Guid.NewGuid();
            var rule = new Rule
            {
                Description = "Orphan rule",
                Expression = "true",
                DependsOnRuleId = missingId
            };

            var availableIds = new List<Guid>(); // Empty — dependency not available
            var act = () => rule.Validate(availableIds);
            act.Should().Throw<RuleValidationException>()
                .WithMessage($"*depends on rule {missingId}*");
        }

        [Fact]
        public void Validate_WithoutAvailableIds_DoesNotCheckDependencies()
        {
            var missingId = Guid.NewGuid();
            var rule = new Rule
            {
                Description = "Standalone rule",
                Expression = "true",
                DependsOnRuleId = missingId
            };

            // No availableIds provided — dependency check is skipped
            var act = () => rule.Validate();
            act.Should().NotThrow();
        }

        [Fact]
        public void ValidateAll_WithMissingDependency_ReturnsError()
        {
            var missingId = Guid.NewGuid();
            var rule = new Rule
            {
                Description = "Orphan rule",
                Expression = "true",
                DependsOnRuleId = missingId
            };

            var availableIds = new List<Guid>();
            var errors = rule.ValidateAll(availableIds);

            errors.Should().HaveCount(1);
            errors[0].ErrorType.Should().Be(ValidationErrorType.MissingDependency);
            errors[0].Message.Should().Contain($"depends on rule {missingId}");
        }

        [Fact]
        public void Workflow_ValidateAll_CatchesMissingDependency()
        {
            var ruleA = new Rule { Description = "A", Expression = "true" };
            var ruleB = new Rule
            {
                Description = "B",
                Expression = "true",
                DependsOnRuleId = Guid.NewGuid() // Non-existent ID
            };

            var workflow = new Workflow
            {
                Rules = { ruleA, ruleB }
            };

            var errors = workflow.ValidateAll();

            errors.Should().Contain(e => e.ErrorType == ValidationErrorType.MissingDependency);
        }
    }
}
