using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Models;
using RoslynRules.Templates;
using System;
using System.Collections.Generic;
using Xunit;

namespace RoslynRules.Tests.Templates
{
    /// <summary>
    /// Tests for RuleTemplate placeholder substitution, instantiation, and edge cases.
    /// </summary>
    public class RuleTemplateTests
    {
        private readonly ExpressionCompiler _compiler;
        private readonly RuleParameter[] _parameters;
        private readonly string[] _assemblies;

        public RuleTemplateTests()
        {
            _compiler = TestCompiler.Instance;
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
            _assemblies = new[] { "RoslynRules.Tests", "System" };
        }

        // ==================== Instantiation ====================

        [Fact]
        public void Instantiate_SimpleValuePlaceholder_CompilesAndExecutes()
        {
            var template = new RuleTemplate
            {
                Description = "Age check",
                Expression = "customer.Age >= {minAge}",
                Placeholders = { ["minAge"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["minAge"] = 18 },
                _compiler, _parameters, _assemblies);

            rule.Description.Should().Be("Age check");
            rule.Expression.Should().Be("customer.Age >= 18");
            rule.IsActive.Should().BeTrue();

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Instantiate_TypePlaceholder_CompilesAndExecutes()
        {
            var template = new RuleTemplate
            {
                Description = "Type check",
                Expression = "typeof({entity}) != null",
                Placeholders = { ["entity"] = PlaceholderKind.Type }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["entity"] = typeof(TestCustomer) },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("typeof(RoslynRules.Tests.TestCustomer) != null");

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Instantiate_IdentifierPlaceholder_CompilesAndExecutes()
        {
            var template = new RuleTemplate
            {
                Description = "Property check",
                Expression = "{param}.Age >= 18",
                Placeholders = { ["param"] = PlaceholderKind.Identifier }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["param"] = "customer" },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("customer.Age >= 18");

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Instantiate_MixedPlaceholders_CompilesAndExecutes()
        {
            var template = new RuleTemplate
            {
                Description = "Complex check",
                Expression = "{entity}.{property} >= {minValue}",
                Placeholders =
                {
                    ["entity"] = PlaceholderKind.Identifier,
                    ["property"] = PlaceholderKind.Identifier,
                    ["minValue"] = PlaceholderKind.Value
                }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object>
                {
                    ["entity"] = "customer",
                    ["property"] = "Age",
                    ["minValue"] = 21
                },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("customer.Age >= 21");

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Instantiate_WithAction_CompilesAndExecutes()
        {
            var template = new RuleTemplate
            {
                Description = "Set adult flag",
                Expression = "customer.Age >= {minAge}",
                Action = "customer.IsAdult = true",
                Placeholders = { ["minAge"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["minAge"] = 18 },
                _compiler, _parameters, _assemblies);

            rule.Action.Should().Be("customer.IsAdult = true");

            var result = rule.Execute(_parameters);
            result.Success.Should().BeTrue();
            ((TestCustomer)_parameters[0].Value!).IsAdult.Should().BeTrue();
        }

        // ==================== Placeholder Extraction ====================

        [Fact]
        public void ExtractPlaceholders_SinglePlaceholder_ReturnsName()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Age >= {minAge}"
            };

            var placeholders = template.ExtractPlaceholders();

            placeholders.Should().ContainSingle().Which.Should().Be("minAge");
        }

        [Fact]
        public void ExtractPlaceholders_MultiplePlaceholders_ReturnsAll()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity}.{property} >= {minValue}"
            };

            var placeholders = template.ExtractPlaceholders();

            placeholders.Should().BeEquivalentTo("entity", "property", "minValue");
        }

        [Fact]
        public void ExtractPlaceholders_Duplicates_ReturnsDistinct()
        {
            var template = new RuleTemplate
            {
                Expression = "{x} + {x} > {y}"
            };

            var placeholders = template.ExtractPlaceholders();

            placeholders.Should().BeEquivalentTo("x", "y");
        }

        [Fact]
        public void ExtractPlaceholders_None_ReturnsEmpty()
        {
            var template = new RuleTemplate
            {
                Expression = "true"
            };

            var placeholders = template.ExtractPlaceholders();

            placeholders.Should().BeEmpty();
        }

        // ==================== Value Formatting ====================

        [Fact]
        public void Instantiate_StringValue_EscapesQuotes()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Name == {name}",
                Placeholders = { ["name"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["name"] = "O\"Connor" },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("customer.Name == \"O\\\"Connor\"");
        }

        [Fact]
        public void Instantiate_BoolValue_FormatsAsLiteral()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.IsAdult == {flag}",
                Placeholders = { ["flag"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["flag"] = true },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("customer.IsAdult == true");
        }

        [Fact]
        public void Instantiate_FloatValue_AddsSuffix()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Age >= {threshold}",
                Placeholders = { ["threshold"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["threshold"] = 3.14f },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("customer.Age >= 3.14f");
        }

        [Fact]
        public void Instantiate_GuidValue_FormatsAsParse()
        {
            var template = new RuleTemplate
            {
                Expression = "Guid.NewGuid() == {id}",
                Placeholders = { ["id"] = PlaceholderKind.Value }
            };

            var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
            var rule = template.Instantiate(
                new Dictionary<string, object> { ["id"] = guid },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("Guid.NewGuid() == Guid.Parse(\"550e8400-e29b-41d4-a716-446655440000\")");
        }

        [Fact]
        public void Instantiate_NullValue_FormatsAsNull()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Name == {name}",
                Placeholders = { ["name"] = PlaceholderKind.Value }
            };

            var rule = template.Instantiate(
                new Dictionary<string, object> { ["name"] = null! },
                _compiler, _parameters, _assemblies);

            rule.Expression.Should().Be("customer.Name == null");
        }

        // ==================== Error Cases ====================

        [Fact]
        public void Instantiate_MissingPlaceholder_ThrowsArgumentException()
        {
            var template = new RuleTemplate
            {
                Expression = "customer.Age >= {minAge}",
                Placeholders = { ["minAge"] = PlaceholderKind.Value }
            };

            var act = () => template.Instantiate(
                new Dictionary<string, object>(), // Empty — missing minAge
                _compiler, _parameters, _assemblies);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*Missing placeholder values: minAge*");
        }

        [Fact]
        public void Instantiate_EmptyExpression_ThrowsInvalidOperationException()
        {
            var template = new RuleTemplate();

            var act = () => template.Instantiate(
                new Dictionary<string, object>(),
                _compiler, _parameters, _assemblies);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Template Expression is not set*");
        }

        [Fact]
        public void Instantiate_WrongTypeForTypePlaceholder_ThrowsArgumentException()
        {
            var template = new RuleTemplate
            {
                Expression = "{entity} != null",
                Placeholders = { ["entity"] = PlaceholderKind.Type }
            };

            var act = () => template.Instantiate(
                new Dictionary<string, object> { ["entity"] = "not a type" },
                _compiler, _parameters, _assemblies);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*requires a System.Type value*");
        }

        [Fact]
        public void Instantiate_WrongTypeForIdentifierPlaceholder_ThrowsArgumentException()
        {
            var template = new RuleTemplate
            {
                Expression = "{param}.Age >= 18",
                Placeholders = { ["param"] = PlaceholderKind.Identifier }
            };

            var act = () => template.Instantiate(
                new Dictionary<string, object> { ["param"] = 123 },
                _compiler, _parameters, _assemblies);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*requires a string value*");
        }
    }
}
