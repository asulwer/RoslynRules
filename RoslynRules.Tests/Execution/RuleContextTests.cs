using RoslynRules.Execution;
using RoslynRules.Models;
using System;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for RuleContext, including TryGetValue pattern.
    /// </summary>
    public class RuleContextTests
    {
        private readonly RuleContext _context = new RuleContext();
        private readonly Guid _ruleId = Guid.NewGuid();

        [Fact]
        public void TryGetValue_RuleSucceeded_ReturnsTrue_WithValue()
        {
            _context.StoreResult(_ruleId, new RuleResult(true, _ruleId, "Test", true, 42));

            bool found = _context.TryGetValue<int>(_ruleId, out var value);

            Assert.True(found);
            Assert.Equal(42, value);
        }

        [Fact]
        public void TryGetValue_RuleSucceeded_WrongType_ReturnsFalse()
        {
            _context.StoreResult(_ruleId, new RuleResult(true, _ruleId, "Test", true, 42));

            bool found = _context.TryGetValue<string>(_ruleId, out var value);

            Assert.False(found);
            Assert.Null(value);
        }

        [Fact]
        public void TryGetValue_RuleFailed_ReturnsFalse()
        {
            _context.StoreResult(_ruleId, new RuleResult(false, _ruleId, "Test", true, Exception: new InvalidOperationException("fail")));

            bool found = _context.TryGetValue<int>(_ruleId, out var value);

            Assert.False(found);
            Assert.Equal(0, value);
        }

        [Fact]
        public void TryGetValue_RuleNotFound_ReturnsFalse()
        {
            bool found = _context.TryGetValue<int>(_ruleId, out var value);

            Assert.False(found);
            Assert.Equal(0, value);
        }

        [Fact]
        public void TryGetValue_ReferenceType_NotFound_ReturnsNull()
        {
            bool found = _context.TryGetValue<string>(_ruleId, out var value);

            Assert.False(found);
            Assert.Null(value);
        }

        [Fact]
        public void GetValue_DistinguishDefaultFromNotFound()
        {
            // Rule succeeded but value is actually 0
            _context.StoreResult(_ruleId, new RuleResult(true, _ruleId, "Test", true, 0));
            int getValueResult = _context.GetValue<int>(_ruleId);
            Assert.Equal(0, getValueResult);

            // Rule not found — also returns 0
            var missingId = Guid.NewGuid();
            int missingResult = _context.GetValue<int>(missingId);
            Assert.Equal(0, missingResult);

            // TryGetValue distinguishes these
            bool found = _context.TryGetValue<int>(_ruleId, out _);
            Assert.True(found);

            bool notFound = _context.TryGetValue<int>(missingId, out _);
            Assert.False(notFound);
        }
    }
}
