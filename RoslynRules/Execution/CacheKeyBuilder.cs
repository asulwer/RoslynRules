using RoslynRules.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Builds cache keys from rule parameters for memoization.
    /// Uses structural, value-based hashing for collections and complex objects (public
    /// readable properties) so that a mutated parameter yields a different key rather than a
    /// stale cache hit. Recursion is bounded by <see cref="MaxCollectionDepth"/>; beyond that
    /// depth it falls back to identity hashing to terminate safely on deep or cyclic graphs.
    /// <para>
    /// NOTE: because value hashing reads public properties, passing an object with lazy-loaded
    /// members (e.g. an EF Core proxy with virtual navigation properties) as a cached parameter
    /// can trigger those loads while the key is built. Prefer plain value/DTO types as cached
    /// rule parameters, or leave <c>CacheDuration</c> unset for such rules.
    /// </para>
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

            // Enums: key by underlying numeric value (distinct per member, no reflected state).
            if (type.IsEnum)
            {
                sb.Append(type.FullName);
                sb.Append('.');
                sb.Append(((Enum)value).ToString("D"));
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

            // Reference/complex types: hash by VALUE (public readable instance properties),
            // so mutating an object between executions produces a different key and does not
            // return a stale cached result. The depth guard bounds recursion on deep or
            // cyclic graphs; beyond it we fall back to identity to terminate safely.
            if (depth >= MaxCollectionDepth)
            {
                sb.Append(type.FullName);
                sb.Append("#ref");
                sb.Append(RuntimeHelpers.GetHashCode(value));
                return;
            }

            AppendObjectByValue(sb, value, type, depth + 1);
        }

        /// <summary>
        /// Appends a structural, value-based representation of an object's public readable
        /// instance properties. Properties are ordered by name for a stable key. Indexers and
        /// properties whose getters throw are skipped.
        /// </summary>
        [UnconditionalSuppressMessage("Trimming", "IL2070",
            Justification = "Value-based cache keys reflect over parameter properties. This runs only at " +
                "execution time for rules that were compiled via Roslyn (already a non-AOT, reflection-" +
                "dependent path). Result caching for reference-type parameters is not supported under trimming/AOT.")]
        private static void AppendObjectByValue(StringBuilder sb, object value, Type type, int depth)
        {
            sb.Append(type.FullName);
            sb.Append('{');

            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal);

            bool first = true;
            foreach (var prop in properties)
            {
                object? propValue;
                try
                {
                    propValue = prop.GetValue(value);
                }
                catch
                {
                    // A getter that throws must not break cache-key construction.
                    continue;
                }

                if (!first) sb.Append(';');
                sb.Append(prop.Name);
                sb.Append('=');
                AppendValue(sb, propValue, depth);
                first = false;
            }

            sb.Append('}');
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
