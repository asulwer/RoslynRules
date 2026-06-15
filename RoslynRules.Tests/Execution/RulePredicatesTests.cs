using RoslynRules.Models;
using RoslynRules.Predicates;
using System;
using System.Collections.Generic;
using Xunit;
using ExpressionCompiler = global::RoslynRules.Compiler.ExpressionCompiler;

namespace RoslynRules.Tests.Execution
{
    public class RulePredicatesTests
    {
        private readonly ExpressionCompiler _compiler = new ExpressionCompiler();

        // ==================== NULL / EMPTY ====================

        [Fact]
        public void IsNotNull_StringValue_Passes()
        {
            var rule = RulePredicates.IsNotNull("name");
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void IsNotNull_NullValue_Fails()
        {
            var rule = RulePredicates.IsNotNull("name");
            var param = new RuleParameter("name", typeof(string), null);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void IsNotNullOrEmpty_EmptyString_Fails()
        {
            var rule = RulePredicates.IsNotNullOrEmpty("name");
            var param = new RuleParameter("name", typeof(string), "");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void IsNotNullOrWhiteSpace_Whitespace_Fails()
        {
            var rule = RulePredicates.IsNotNullOrWhiteSpace("name");
            var param = new RuleParameter("name", typeof(string), "   ");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void IsNotEmpty_NonEmptyCollection_Passes()
        {
            var rule = RulePredicates.IsNotEmpty("items");
            var param = new RuleParameter("items", typeof(List<int>), new List<int> { 1, 2, 3 });
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void IsEmpty_EmptyCollection_Passes()
        {
            var rule = RulePredicates.IsEmpty("items");
            var param = new RuleParameter("items", typeof(List<int>), new List<int>());
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        // ==================== COMPARISON ====================

        [Fact]
        public void GreaterThan_ValueAbove_Passes()
        {
            var rule = RulePredicates.GreaterThan("age", 18);
            var param = new RuleParameter("age", typeof(int), 25);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void GreaterThan_ValueBelow_Fails()
        {
            var rule = RulePredicates.GreaterThan("age", 18);
            var param = new RuleParameter("age", typeof(int), 15);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void GreaterThanOrEqual_EqualValue_Passes()
        {
            var rule = RulePredicates.GreaterThanOrEqual("age", 18);
            var param = new RuleParameter("age", typeof(int), 18);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void LessThan_ValueBelow_Passes()
        {
            var rule = RulePredicates.LessThan("age", 18);
            var param = new RuleParameter("age", typeof(int), 15);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void InRange_ValueInside_Passes()
        {
            var rule = RulePredicates.InRange("age", 18, 65);
            var param = new RuleParameter("age", typeof(int), 30);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void InRange_ValueOutside_Fails()
        {
            var rule = RulePredicates.InRange("age", 18, 65);
            var param = new RuleParameter("age", typeof(int), 100);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void Equals_SameValue_Passes()
        {
            var rule = RulePredicates.Equals("status", "active");
            var param = new RuleParameter("status", typeof(string), "active");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void NotEquals_DifferentValue_Passes()
        {
            var rule = RulePredicates.NotEquals("status", "deleted");
            var param = new RuleParameter("status", typeof(string), "active");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        // ==================== STRING ====================

        [Fact]
        public void MatchesRegex_ValidEmail_Passes()
        {
            var rule = RulePredicates.MatchesRegex("email", "^[^@]+@[^@]+\\.[^@]+$");
            var param = new RuleParameter("email", typeof(string), "test@example.com");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void MatchesRegex_InvalidEmail_Fails()
        {
            var rule = RulePredicates.MatchesRegex("email", "^[^@]+@[^@]+\\.[^@]+$");
            var param = new RuleParameter("email", typeof(string), "not-an-email");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void Contains_ValuePresent_Passes()
        {
            var rule = RulePredicates.Contains("name", "Ali");
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void StartsWith_CorrectPrefix_Passes()
        {
            var rule = RulePredicates.StartsWith("name", "Ali");
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void EndsWith_CorrectSuffix_Passes()
        {
            var rule = RulePredicates.EndsWith("name", "ce");
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void HasLength_ExactLength_Passes()
        {
            var rule = RulePredicates.HasLength("name", 5);
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void HasMinLength_Shorter_Fails()
        {
            var rule = RulePredicates.HasMinLength("name", 10);
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void HasMaxLength_Longer_Fails()
        {
            var rule = RulePredicates.HasMaxLength("name", 3);
            var param = new RuleParameter("name", typeof(string), "Alice");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        // ==================== COLLECTION ====================

        [Fact]
        public void CountEquals_CorrectCount_Passes()
        {
            var rule = RulePredicates.CountEquals("items", 3);
            var param = new RuleParameter("items", typeof(List<int>), new List<int> { 1, 2, 3 });
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void Contains_ElementPresent_Passes()
        {
            var rule = RulePredicates.Contains("items", 2);
            var param = new RuleParameter("items", typeof(List<int>), new List<int> { 1, 2, 3 });
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        // ==================== BOOLEAN / TYPE ====================

        [Fact]
        public void IsTrue_TrueValue_Passes()
        {
            var rule = RulePredicates.IsTrue("flag");
            var param = new RuleParameter("flag", typeof(bool), true);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void IsFalse_FalseValue_Passes()
        {
            var rule = RulePredicates.IsFalse("flag");
            var param = new RuleParameter("flag", typeof(bool), false);
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void IsOfType_MatchingType_Passes()
        {
            var rule = RulePredicates.IsOfType<string>("value");
            var param = new RuleParameter("value", typeof(object), "hello");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.True(result.Success);
        }

        [Fact]
        public void IsOfType_WrongType_Fails()
        {
            var rule = RulePredicates.IsOfType<int>("value");
            var param = new RuleParameter("value", typeof(object), "hello");
            rule.Compile(_compiler, new[] { param });
            var result = rule.Execute(param);
            Assert.False(result.Success);
        }

        [Fact]
        public void CustomDescription_IsSet()
        {
            var rule = RulePredicates.GreaterThan("age", 18, "Must be adult");
            Assert.Equal("Must be adult", rule.Description);
        }

        [Fact]
        public void DefaultDescription_IsGenerated()
        {
            var rule = RulePredicates.GreaterThan("age", 18);
            Assert.Equal("age > 18", rule.Description);
        }
    }
}



