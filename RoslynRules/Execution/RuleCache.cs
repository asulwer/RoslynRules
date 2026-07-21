using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Thread-safe cache for rule evaluation results.
    /// Stores results with automatic expiration. Expired entries are evicted lazily on read
    /// and, to bound memory for high-cardinality keys, a size cap triggers a sweep of expired
    /// entries (falling back to trimming the oldest entries) once the cap is exceeded.
    /// </summary>
    internal sealed class RuleCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly int _maxEntries;

        /// <summary>
        /// Creates a cache with an optional maximum entry count. When the count exceeds the cap,
        /// expired entries are swept; if still over the cap, the oldest entries are trimmed.
        /// </summary>
        public RuleCache(int maxEntries = 10_000)
        {
            _maxEntries = maxEntries > 0 ? maxEntries : 10_000;
        }

        /// <summary>
        /// Attempts to retrieve a cached result that has not expired.
        /// Expired entries encountered on read are removed.
        /// </summary>
        public bool TryGet(string key, [NotNullWhen(true)] out Models.RuleResult result)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.Expires > DateTime.UtcNow)
                {
                    result = entry.Result;
                    return true;
                }

                // Lazily evict the expired entry.
                _cache.TryRemove(key, out _);
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Stores a result in the cache with the specified duration.
        /// </summary>
        public void Set(string key, Models.RuleResult result, TimeSpan duration)
        {
            _cache[key] = new CacheEntry(result, DateTime.UtcNow.Add(duration));

            if (_cache.Count > _maxEntries)
                Evict();
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public void Clear() => _cache.Clear();

        /// <summary>
        /// Returns the number of cached entries (including any not-yet-swept expired entries).
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Bounds memory: first drop expired entries; if still over the cap, trim the entries
        /// closest to expiry. Best-effort and non-blocking — concurrent writers may briefly push
        /// the count above the cap between sweeps.
        /// </summary>
        private void Evict()
        {
            var now = DateTime.UtcNow;

            foreach (var kvp in _cache)
            {
                if (kvp.Value.Expires <= now)
                    _cache.TryRemove(kvp.Key, out _);
            }

            var overflow = _cache.Count - _maxEntries;
            if (overflow <= 0)
                return;

            // Still over the cap with live entries: trim those nearest expiry.
            var oldest = _cache
                .OrderBy(kvp => kvp.Value.Expires)
                .Take(overflow)
                .Select(kvp => kvp.Key)
                .ToArray();

            foreach (var key in oldest)
                _cache.TryRemove(key, out _);
        }

        private readonly record struct CacheEntry(Models.RuleResult Result, DateTime Expires);
    }
}
