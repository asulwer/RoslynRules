using FluentAssertions;
using RoslynRules.Predicates;
using System;
using Xunit;

namespace RoslynRules.Tests.Predicates
{
    /// <summary>
    /// Tests for RulePredicates parameter name validation.
    /// </summary>
    public class RulePredicatesValidationTests
    {
        [Fact]
        public void IsNotNull_ValidIdentifier_Succeeds()
        {
            var act = () => RulePredicates.IsNotNull("customer");
            act.Should().NotThrow();
        }

        [Fact]
        public void IsNotNull_DottedPath_Succeeds()
        {
            var act = () => RulePredicates.IsNotNull("order.CustomerId");
            act.Should().NotThrow();
        }

        [Fact]
        public void IsNotNull_DeeplyNestedPath_Succeeds()
        {
            var act = () => RulePredicates.IsNotNull("order.Customer.Address.ZipCode");
            act.Should().NotThrow();
        }

        [Fact]
        public void IsNotNull_EmptyName_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull("");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Parameter name cannot be null or empty*");
        }

        [Fact]
        public void IsNotNull_InvalidCharacters_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull("customer-name");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*not a valid C# identifier*");
        }

        [Fact]
        public void IsNotNull_SpaceInName_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull("customer name");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*not a valid C# identifier*");
        }

        [Fact]
        public void IsNotNull_DoubleDots_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull("customer..name");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*empty segments*");
        }

        [Fact]
        public void IsNotNull_LeadingDot_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull(".customer");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*empty segments*");
        }

        [Fact]
        public void IsNotNull_TrailingDot_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull("customer.");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*empty segments*");
        }

        [Fact]
        public void IsNotNull_NumericStart_ThrowsArgumentException()
        {
            var act = () => RulePredicates.IsNotNull("123abc");
            act.Should().Throw<ArgumentException>()
                .WithMessage("*not a valid C# identifier*");
        }
    }
}
