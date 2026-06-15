using RoslynRules.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Builds cache keys from rule parameters for memoization.
    /// Uses structural hashing for collections and identity hashing for
    /// mutable reference types to prevent stale cache hits.
    /// </summary>
    internal static class CacheKeyBuilder
    {
        /// <summary>
        /// Maximum recursion depth for collection serialization to prevent
        /// stack overflow on deeply nested or circular structures.
        /// </summary>
        private const int MaxCollectionDepth = 10;

        /// <summary>
        /// Creates a cache key from the rule ID and parameter values.
        /// Uses a fast non-cryptographic hash for performance.
        /// </summary>
        public static string Build(Guid ruleId, RuleParameter[] parameters)
        {
            var sb = new StringBuilder(ruleId.ToString("N"));
            sb.Append(':');

            foreach (var param in parameters)
            {
                sb.Append(param.Name);
                sb.Append('=');
                AppendValue(sb, param.Value, 0);
                sb.Append('|');
            }

            return sb.ToString();
        }

        private static void AppendValue(StringBuilder sb, object? value, int depth)
        {
            if (value is null)
            {
                sb.Append("null");
                return;
            }

            var type = value.GetType();

            // Fast path: primitives and IFormattable
            if (value is IFormattable formattable && IsPrimitiveLike(type))
            {
                sb.Append(formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture));
                return;
            }

            // Strings
            if (value is string str)
            {
                sb.Append('"');
                sb.Append(str);
                sb.Append('"');
                return;
            }

            // Collections (arrays, lists, dictionaries, etc.) — structural hash
            if (value is IEnumerable enumerable && !(value is string))
            {
                if (depth >= MaxCollectionDepth)
                {
                    sb.Append("[maxdepth]");
                    return;
                }
                AppendCollection(sb, enumerable, depth + 1);
                return;
            }

            // Mutable reference types: use identity hash for stable cache keys.
            // RuntimeHelpers.GetHashCode returns the object identity hash code,
            // which is stable across mutations. This prevents stale cache hits
            // when a mutable object's state changes between executions.
            sb.Append(type.FullName);
            sb.Append("#ref");
            sb.Append(RuntimeHelpers.GetHashCode(value));
        }

        private static void AppendCollection(StringBuilder sb, IEnumerable enumerable, int depth)
        {
            sb.Append('[');
            bool first = true;
            foreach (var item in enumerable)
            {
                if (!first) sb.Append(',');
                AppendValue(sb, item, depth);
                first = false;
            }
            sb.Append(']');
        }

        private static bool IsPrimitiveLike(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid)
                || (Nullable.GetUnderlyingType(type) is Type underlying && IsPrimitiveLike(underlying));
        }
    }
}
