using FluentAssertions;
using RoslynRules.Batch;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Linq;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Core
{
    /// <summary>
    /// Tests for ValidateAll() — non-throwing validation that returns all errors.
    /// </summary>
    public class ValidateAllTests
    {
        [Fact]
        public void Workflow_ValidateAll_EmptyWorkflow_ReturnsNoActiveRulesError()
        {
            var wf = new global::RoslynRules.Models.Workflow();
            var errors = wf.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.NoActiveRules, errors[0].ErrorType);
        }

        [Fact]
        public void Workflow_ValidateAll_ValidWorkflow_ReturnsEmpty()
        {
            var wf = new global::RoslynRules.Models.Workflow();
            wf.Rules.Add(new Rule { Expression = "true", Description = "Valid" });

            var errors = wf.ValidateAll();

            Assert.Empty(errors);
        }

        [Fact]
        public void Workflow_ValidateAll_InvalidRule_ReturnsSyntaxError()
        {
            var wf = new global::RoslynRules.Models.Workflow();
            wf.Rules.Add(new Rule { Expression = "invalid syntax here @#$", Description = "Broken" });

            var errors = wf.ValidateAll();

            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.SyntaxError);
        }

        [Fact]
        public void Workflow_ValidateAll_MultipleErrors_ReturnsAll()
        {
            var wf = new global::RoslynRules.Models.Workflow();
            var sharedId = Guid.NewGuid();
            var rule1 = new Rule(sharedId) { Expression = "true" };
            var rule2 = new Rule(sharedId) { Expression = "true" }; // Duplicate ID
            var rule3 = new Rule { Expression = "broken syntax @#$" };
            wf.Rules.Add(rule1);
            wf.Rules.Add(rule2);
            wf.Rules.Add(rule3);

            var errors = wf.ValidateAll();

            Assert.True(errors.Length >= 2, $"Expected 2+ errors but got {errors.Length}");
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.DuplicateRuleId);
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.SyntaxError);
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.DuplicateRuleId);
            Assert.Contains(errors, e => e.ErrorType == ValidationErrorType.SyntaxError);
        }

        [Fact]
        public void Rule_ValidateAll_EmptyRule_ReturnsEmptyRuleError()
        {
            var rule = new Rule { Description = "Empty" };

            var errors = rule.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.EmptyRule, errors[0].ErrorType);
        }

        [Fact]
        public void Rule_ValidateAll_InvalidExpression_ReturnsSyntaxError()
        {
            var rule = new Rule { Expression = "broken @#$", Description = "Bad" };

            var errors = rule.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.SyntaxError, errors[0].ErrorType);
        }

        [Fact]
        public void RuleBatch_ValidateAll_EmptyBatch_ReturnsNoActiveRulesError()
        {
            var batch = new RuleBatch();

            var errors = batch.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.NoActiveRules, errors[0].ErrorType);
        }

        // ==================== STATIC VALIDATE SEMANTICS OVERLOADS ====================

        [Fact]
        public void Rule_ValidateSemantics_Static_WithType_ValidExpression_Succeeds()
        {
            // Should not throw
            Rule.ValidateSemantics("param > 0", typeof(int));
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_WithType_InvalidExpression_Throws()
        {
            var ex = Assert.Throws<RuleCompilationException>(() =>
                Rule.ValidateSemantics("param.NonExistentMethod()", typeof(int)));
            Assert.Contains("Semantic error", ex.Message);
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_WithTypeName_Alias_ValidExpression_Succeeds()
        {
            // Should not throw
            Rule.ValidateSemantics("param.Length > 0", "string");
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_WithTypeName_FullName_ValidExpression_Succeeds()
        {
            // Should not throw
            Rule.ValidateSemantics("param.Year > 2000", "System.DateTime");
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_WithTypeName_InvalidTypeName_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                Rule.ValidateSemantics("param > 0", "NonExistent.Type"));
            Assert.Contains("Could not resolve type", ex.Message);
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_WithTypeName_CustomParameterName_Succeeds()
        {
            // Should not throw — uses custom parameter name with matching type
            Rule.ValidateSemantics("customer > 0", "int", "customer");
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_NullExpression_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                Rule.ValidateSemantics(null!, typeof(int)));
            Assert.Contains("Expression cannot be null", ex.Message);
        }

        [Fact]
        public void Rule_ValidateSemantics_Static_EmptyExpression_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                Rule.ValidateSemantics("   ", typeof(int)));
            Assert.Contains("Expression cannot be null", ex.Message);
        }

        [Fact]
        public void RuleBatch_ValidateAll_DuplicateIds_ReturnsDuplicateRuleIdError()
        {
            var batch = new RuleBatch();
            var sharedId = Guid.NewGuid();
            var rule1 = new Rule(sharedId) { Expression = "true" };
            var rule2 = new Rule(sharedId) { Expression = "false" };
            batch.AddRule(rule1);
            batch.AddRule(rule2);

            var errors = batch.ValidateAll();

            Assert.Single(errors);
            Assert.Equal(ValidationErrorType.DuplicateRuleId, errors[0].ErrorType);
        }

        [Fact]
        public void IRuleEngine_ValidateAll_ImplementedByWorkflow()
        {
            RoslynRules.Abstractions.IRuleEngine engine = new global::RoslynRules.Models.Workflow();
            engine = new global::RoslynRules.Models.Workflow();
            ((Workflow)engine).Rules.Add(new Rule { Expression = "true" });

            var errors = engine.ValidateAll();

            Assert.Empty(errors);
        }

        [Fact]
        public void IRuleEngine_ValidateAll_ImplementedByRuleBatch()
        {
            RoslynRules.Abstractions.IRuleEngine engine = new RuleBatch();
            ((RuleBatch)engine).AddRule(new Rule { Expression = "true" });

            var errors = engine.ValidateAll();

            Assert.Empty(errors);
        }
    }
}