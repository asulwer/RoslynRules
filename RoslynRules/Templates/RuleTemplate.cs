using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RoslynRules.Templates
{
    /// <summary>
    /// A reusable rule template with placeholders that can be instantiated
    /// into concrete compiled rules.
    /// </summary>
    public class RuleTemplate
    {
        /// <summary>
        /// Human-readable description of what this template validates.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Boolean expression containing placeholders like {entity}.Age >= {minAge}.
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Optional action expression (also supports placeholders).
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// Placeholder metadata: name -> kind (Type or Value).
        /// </summary>
        public Dictionary<string, PlaceholderKind> Placeholders { get; } = new();

        /// <summary>
        /// Creates a compiled Rule by substituting placeholders with actual values.
        /// </summary>
        /// <param name="values">Dictionary of placeholder names to their substituted values.</param>
        /// <param name="compiler">Expression compiler for compiling the resulting rule.</param>
        /// <param name="parameters">Runtime parameters for rule compilation.</param>
        /// <param name="assemblies">Additional assembly references for compilation.</param>
        /// <returns>A compiled Rule ready for execution.</returns>
        public Rule Instantiate(
            Dictionary<string, object> values,
            ExpressionCompiler compiler,
            RuleParameter[] parameters,
            string[] assemblies,
            Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            if (string.IsNullOrWhiteSpace(Expression))
                throw new InvalidOperationException("Template Expression is not set.");

            // Validate all placeholders are provided
            var missing = Placeholders.Keys.Except(values.Keys, StringComparer.OrdinalIgnoreCase).ToList();
            if (missing.Count > 0)
            {
                throw new ArgumentException(
                    $"Missing placeholder values: {string.Join(", ", missing)}. " +
                    $"Expected: {string.Join(", ", Placeholders.Keys)}.",
                    nameof(values));
            }

            // Substitute placeholders in expression
            var substitutedExpression = Substitute(Expression, values);

            // Substitute placeholders in action (if present)
            string? substitutedAction = null;
            if (!string.IsNullOrWhiteSpace(Action))
                substitutedAction = Substitute(Action!, values);

            // Create and compile the rule
            var rule = new Rule
            {
                Description = Description,
                Expression = substitutedExpression,
                Action = substitutedAction ?? string.Empty,
                IsActive = true
            };

            rule.Compile(compiler, parameters, assemblies, referenceProvider);
            return rule;
        }

        /// <summary>
        /// Extracts placeholder names from the expression without compiling.
        /// </summary>
        public IReadOnlyList<string> ExtractPlaceholders()
        {
            var matches = PlaceholderRegex.Matches(Expression);
            return matches.Select(m => m.Groups[1].Value).Distinct().ToList();
        }

        private string Substitute(string template, Dictionary<string, object> values)
        {
            var result = template;

            foreach (var (name, kind) in Placeholders)
            {
                var value = values[name];
                var replacement = kind switch
                {
                    PlaceholderKind.Type => FormatType(value),
                    PlaceholderKind.Identifier => FormatIdentifier(value),
                    PlaceholderKind.Value => FormatValue(value),
                    _ => throw new InvalidOperationException($"Unknown placeholder kind: {kind}")
                };

                result = result.Replace($"{{{name}}}", replacement, StringComparison.Ordinal);
            }

            return result;
        }

        private static string FormatType(object value)
        {
            if (value is Type type)
                return type.FullName ?? type.Name;

            throw new ArgumentException(
                $"Placeholder of kind '{PlaceholderKind.Type}' requires a System.Type value. " +
                $"Received: {value?.GetType().Name ?? "null"}.");
        }

        private static string FormatIdentifier(object value)
        {
            if (value is string identifier)
                return identifier;

            throw new ArgumentException(
                $"Placeholder of kind '{PlaceholderKind.Identifier}' requires a string value. " +
                $"Received: {value?.GetType().Name ?? "null"}.");
        }

        private static string FormatValue(object value)
        {
            if (value is null)
                return "null";

            if (value is string str)
                return $"\"{str.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

            if (value is bool b)
                return b ? "true" : "false";

            if (value is char c)
                return $"'{(c == '\'' ? "\\'" : c.ToString())}'";

            if (value is float f)
                return $"{f}f";

            if (value is double d)
                return $"{d}d";

            if (value is decimal m)
                return $"{m}m";

            if (value is DateTime dt)
                return $"DateTime.Parse(\"{dt:O}\")";

            if (value is DateTimeOffset dto)
                return $"DateTimeOffset.Parse(\"{dto:O}\")";

            if (value is TimeSpan ts)
                return $"TimeSpan.Parse(\"{ts}\")";

            if (value is Guid g)
                return $"Guid.Parse(\"{g:D}\")";

            if (value is Enum e)
                return $"{value.GetType().FullName}.{e}";

            // For numeric types and everything else, use invariant culture ToString
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        }

        private static readonly Regex PlaceholderRegex = new(
            @"\{(\w+)\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
