using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Thread-safe cache for rule evaluation results.
    /// Stores results with automatic expiration.
    /// </summary>
    internal sealed class RuleCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

        /// <summary>
        /// Attempts to retrieve a cached result that has not expired.
        /// </summary>
        public bool TryGet(string key, [NotNullWhen(true)] out Models.RuleResult result)
        {
            if (_cache.TryGetValue(key, out var entry) && entry.Expires > DateTime.UtcNow)
            {
                result = entry.Result;
                return true;
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
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public void Clear() => _cache.Clear();

        /// <summary>
        /// Returns the number of cached entries (including expired).
        /// </summary>
        public int Count => _cache.Count;

        private readonly record struct CacheEntry(Models.RuleResult Result, DateTime Expires);
    }
}
