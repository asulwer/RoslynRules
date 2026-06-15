using FluentAssertions;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for rule description localization (i18n) via IRuleDescriptionProvider.
    /// </summary>
    public class RuleLocalizationTests
    {
        [Fact]
        public void GetLocalizedDescription_NoKey_ReturnsDescription()
        {
            var rule = new Rule
            {
                Description = "English description"
            };

            var result = rule.GetLocalizedDescription();

            result.Should().Be("English description");
        }

        [Fact]
        public void GetLocalizedDescription_WithKeyAndProvider_ReturnsLocalized()
        {
            var rule = new Rule
            {
                Description = "English description",
                DescriptionKey = "rule.adultCheck",
                DescriptionProvider = new TestDescriptionProvider()
            };

            var result = rule.GetLocalizedDescription();

            result.Should().Be("Vérification adulte");
        }

        [Fact]
        public void GetLocalizedDescription_WithKeyNoProvider_ReturnsDescription()
        {
            var rule = new Rule
            {
                Description = "English description",
                DescriptionKey = "rule.adultCheck"
            };

            var result = rule.GetLocalizedDescription();

            result.Should().Be("English description");
        }

        [Fact]
        public void GetLocalizedDescription_KeyNotFound_ReturnsDescription()
        {
            var rule = new Rule
            {
                Description = "English description",
                DescriptionKey = "rule.nonExistent",
                DescriptionProvider = new TestDescriptionProvider()
            };

            var result = rule.GetLocalizedDescription();

            result.Should().Be("English description");
        }

        [Fact]
        public void GetLocalizedDescription_WithCulture_ReturnsLocalized()
        {
            var rule = new Rule
            {
                Description = "English description",
                DescriptionKey = "rule.adultCheck",
                DescriptionProvider = new TestDescriptionProvider()
            };

            var french = rule.GetLocalizedDescription("fr-FR");
            var spanish = rule.GetLocalizedDescription("es-ES");
            var fallback = rule.GetLocalizedDescription("de-DE");

            french.Should().Be("Vérification adulte");
            spanish.Should().Be("Verificación de adultos");
            fallback.Should().Be("English description"); // falls back to Description
        }

        [Fact]
        public void GetLocalizedDescription_EmptyKey_ReturnsDescription()
        {
            var rule = new Rule
            {
                Description = "English description",
                DescriptionKey = "",
                DescriptionProvider = new TestDescriptionProvider()
            };

            var result = rule.GetLocalizedDescription();

            result.Should().Be("English description");
        }

        [Fact]
        public void DescriptionKey_Set_AfterCompile_Throws()
        {
            var rule = new Rule
            {
                Expression = "x > 0",
                IsActive = true
            };

            var parameters = new[] { new RuleParameter("x", typeof(int), 1) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            Action act = () => rule.DescriptionKey = "rule.test";

            act.Should().Throw<RuleCompilationException>();
        }

        [Fact]
        public void Execute_ResultContainsLocalizedDescription()
        {
            var rule = new Rule
            {
                Description = "English",
                DescriptionKey = "rule.adultCheck",
                DescriptionProvider = new TestDescriptionProvider(),
                Expression = "x >= 18",
                IsActive = true
            };

            var parameters = new[] { new RuleParameter("x", typeof(int), 25) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            var result = rule.Execute(parameters);

            result.RuleDescription.Should().Be("Vérification adulte");
        }

        [Fact]
        public void Execute_NoProvider_ResultContainsDefaultDescription()
        {
            var rule = new Rule
            {
                Description = "Age check",
                Expression = "x >= 18",
                IsActive = true
            };

            var parameters = new[] { new RuleParameter("x", typeof(int), 25) };
            rule.Compile(new global::RoslynRules.Compiler.ExpressionCompiler(), parameters);

            var result = rule.Execute(parameters);

            result.RuleDescription.Should().Be("Age check");
        }

        /// <summary>
        /// Test implementation of IRuleDescriptionProvider for unit tests.
        /// </summary>
        private class TestDescriptionProvider : IRuleDescriptionProvider
        {
            private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
            {
                ["rule.adultCheck"] = new()
                {
                    ["fr-FR"] = "Vérification adulte",
                    ["es-ES"] = "Verificación de adultos"
                }
            };

            public string? GetDescription(string key, string? culture = null)
            {
                if (_translations.TryGetValue(key, out var cultureMap))
                {
                    if (culture != null && cultureMap.TryGetValue(culture, out var localized))
                        return localized;

                    // Only fallback to first available if no specific culture was requested
                    if (culture == null)
                        return cultureMap.Values.FirstOrDefault();
                }

                return null;
            }
        }
    }
}
