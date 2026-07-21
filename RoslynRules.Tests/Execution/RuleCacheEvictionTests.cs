using FluentAssertions;
using RoslynRules.Execution;
using RoslynRules.Models;
using System;
using System.Threading;
using Xunit;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests for the internal <see cref="RuleCache"/> eviction behavior: expired entries are
    /// removed on read, and a size cap bounds memory for high-cardinality keys.
    /// </summary>
    public class RuleCacheEvictionTests
    {
        private static RuleResult Result() => new RuleResult(true, Guid.NewGuid(), "r", true);

        [Fact]
        public void TryGet_ExpiredEntry_IsEvicted()
        {
            var cache = new RuleCache();
            cache.Set("k", Result(), TimeSpan.FromMilliseconds(20));
            cache.Count.Should().Be(1);

            Thread.Sleep(60); // let it expire

            cache.TryGet("k", out _).Should().BeFalse();
            cache.Count.Should().Be(0, "an expired entry should be removed when read");
        }

        [Fact]
        public void Set_ExceedingCap_BoundsEntryCount()
        {
            var cache = new RuleCache(maxEntries: 5);

            for (int i = 0; i < 50; i++)
                cache.Set($"key-{i}", Result(), TimeSpan.FromMinutes(5));

            cache.Count.Should().BeLessThanOrEqualTo(5,
                "the size cap must trim entries so the cache does not grow unbounded");
        }

        [Fact]
        public void Set_ExceedingCap_PrefersEvictingExpiredEntries()
        {
            var cache = new RuleCache(maxEntries: 3);

            // Three already-expired entries.
            for (int i = 0; i < 3; i++)
                cache.Set($"expired-{i}", Result(), TimeSpan.FromMilliseconds(10));

            Thread.Sleep(40);

            // Adding live entries past the cap should sweep the expired ones first.
            for (int i = 0; i < 3; i++)
                cache.Set($"live-{i}", Result(), TimeSpan.FromMinutes(5));

            cache.Count.Should().BeLessThanOrEqualTo(3);
            cache.TryGet("live-0", out _).Should().BeTrue();
            cache.TryGet("live-2", out _).Should().BeTrue();
        }
    }
}
