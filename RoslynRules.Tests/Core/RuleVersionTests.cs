using FluentAssertions;
using RoslynRules.Models;
using System;
using Xunit;

namespace RoslynRules.Tests.Core
{
    public class RuleVersionTests
    {
        [Theory]
        [InlineData(1, 0, 0, "1.0.0")]
        [InlineData(2, 3, 4, "2.3.4")]
        [InlineData(1, 0, 0, "1.0.0-alpha", "alpha")]
        [InlineData(1, 0, 0, "1.0.0+build.123", null, "build.123")]
        [InlineData(1, 0, 0, "1.0.0-alpha+build.123", "alpha", "build.123")]
        public void ToString_FormatsCorrectly(int major, int minor, int patch, string expected, string? prerelease = null, string? build = null)
        {
            var version = new RuleVersion(major, minor, patch, prerelease, build);
            version.ToString().Should().Be(expected);
        }

        [Theory]
        [InlineData("1.0.0", 1, 0, 0, null, null)]
        [InlineData("2.3.4", 2, 3, 4, null, null)]
        [InlineData("1.0.0-alpha", 1, 0, 0, "alpha", null)]
        [InlineData("1.0.0+build.123", 1, 0, 0, null, "build.123")]
        [InlineData("1.0.0-alpha+build.123", 1, 0, 0, "alpha", "build.123")]
        [InlineData("1", 1, 0, 0, null, null)]
        [InlineData("1.2", 1, 2, 0, null, null)]
        public void Parse_ParsesCorrectly(string input, int major, int minor, int patch, string? prerelease, string? build)
        {
            var version = RuleVersion.Parse(input);
            version.Major.Should().Be(major);
            version.Minor.Should().Be(minor);
            version.Patch.Should().Be(patch);
            version.Prerelease.Should().Be(prerelease);
            version.BuildMetadata.Should().Be(build);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("")]
        [InlineData("1.2.3.4.5")]
        [InlineData("abc")]
        public void Parse_Invalid_ThrowsFormatException(string input)
        {
            Action act = () => RuleVersion.Parse(input);
            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void TryParse_Valid_ReturnsTrue()
        {
            var result = RuleVersion.TryParse("1.2.3", out var version);
            result.Should().BeTrue();
            version.Should().Be(new RuleVersion(1, 2, 3));
        }

        [Fact]
        public void TryParse_Invalid_ReturnsFalse()
        {
            var result = RuleVersion.TryParse("invalid", out var version);
            result.Should().BeFalse();
            version.Should().Be(RuleVersion.Unspecified);
        }

        [Theory]
        [InlineData(1, 0, 0, 2, 0, 0, -1)]   // 1.0.0 < 2.0.0
        [InlineData(2, 0, 0, 1, 0, 0, 1)]    // 2.0.0 > 1.0.0
        [InlineData(1, 1, 0, 1, 0, 0, 1)]    // 1.1.0 > 1.0.0
        [InlineData(1, 0, 1, 1, 0, 0, 1)]    // 1.0.1 > 1.0.0
        [InlineData(1, 0, 0, 1, 0, 0, 0)]    // 1.0.0 == 1.0.0
        [InlineData(1, 0, 0, 1, 0, 0, -1, "alpha")]  // 1.0.0-alpha < 1.0.0
        public void CompareTo_ComparesCorrectly(int maj1, int min1, int patch1, int maj2, int min2, int patch2, int expected, string? pre1 = null, string? pre2 = null)
        {
            var v1 = new RuleVersion(maj1, min1, patch1, pre1);
            var v2 = new RuleVersion(maj2, min2, patch2, pre2);
            v1.CompareTo(v2).Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 0, 0, 1, 0, 0, true)]   // Same major, same version
        [InlineData(1, 1, 0, 1, 0, 0, true)]   // Same major, greater minor
        [InlineData(1, 0, 1, 1, 0, 0, true)]   // Same major, greater patch
        [InlineData(2, 0, 0, 1, 0, 0, false)]  // Different major
        [InlineData(1, 0, 0, 2, 0, 0, false)]  // Different major (reverse)
        public void IsCompatibleWith_ChecksCorrectly(int maj1, int min1, int patch1, int maj2, int min2, int patch2, bool expected)
        {
            var v1 = new RuleVersion(maj1, min1, patch1);
            var v2 = new RuleVersion(maj2, min2, patch2);
            v1.IsCompatibleWith(v2).Should().Be(expected);
        }

        [Fact]
        public void IncrementMajor_IncrementsAndResets()
        {
            var v = new RuleVersion(1, 2, 3);
            v.IncrementMajor().Should().Be(new RuleVersion(2, 0, 0));
        }

        [Fact]
        public void IncrementMinor_IncrementsAndResetsPatch()
        {
            var v = new RuleVersion(1, 2, 3);
            v.IncrementMinor().Should().Be(new RuleVersion(1, 3, 0));
        }

        [Fact]
        public void IncrementPatch_IncrementsPatch()
        {
            var v = new RuleVersion(1, 2, 3);
            v.IncrementPatch().Should().Be(new RuleVersion(1, 2, 4));
        }

        [Fact]
        public void Operators_WorkCorrectly()
        {
            var v1 = new RuleVersion(1, 0, 0);
            var v2 = new RuleVersion(2, 0, 0);

            (v1 < v2).Should().BeTrue();
            (v2 > v1).Should().BeTrue();
            (v1 <= v2).Should().BeTrue();
            (v2 >= v1).Should().BeTrue();
        }
    }
}
