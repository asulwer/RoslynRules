using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Predicates
{
    /// <summary>
    /// Static factory methods for creating common validation rules.
    /// Eliminates the need to write raw C# expressions for frequent patterns.
    /// </summary>
    public static class RulePredicates
    {
        // ==================== NULL / EMPTY CHECKS ====================

        /// <summary>
        /// Creates a rule that passes when the parameter is not null.
        /// </summary>
        /// <param name="parameterName">Name of the parameter to validate. Accepts dotted member-access paths (e.g., "order.CustomerId").</param>
        /// <param name="description">Optional description for the rule.</param>
        public static Rule IsNotNull(string parameterName, string? description = null)
        {
            ValidateParameterName(parameterName);
            return new Rule
            {
                Description = description ?? $"{parameterName} is not null",
                Expression = $"{parameterName} != null"
            };
        }

        /// <summary>
        /// Creates a rule that passes when the string parameter is not null or empty.
        /// </summary>
        public static Rule IsNotNullOrEmpty(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is not null or empty",
                Expression = $"!string.IsNullOrEmpty({parameterName})"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter is not null or whitespace.
        /// </summary>
        public static Rule IsNotNullOrWhiteSpace(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is not null or whitespace",
                Expression = $"!string.IsNullOrWhiteSpace({parameterName})"
            };

        /// <summary>
        /// Creates a rule that passes when the collection parameter is not empty.
        /// </summary>
        public static Rule IsNotEmpty(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is not empty",
                Expression = $"{parameterName}.Any()"
            };

        /// <summary>
        /// Creates a rule that passes when the collection parameter is empty.
        /// </summary>
        public static Rule IsEmpty(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is empty",
                Expression = $"!{parameterName}.Any()"
            };

        // ==================== COMPARISON ====================

        /// <summary>
        /// Creates a rule that passes when the parameter is greater than the specified value.
        /// </summary>
        public static Rule GreaterThan<T>(string parameterName, T value, string? description = null) where T : struct
            => new Rule
            {
                Description = description ?? $"{parameterName} > {value}",
                Expression = $"{parameterName} > {FormatValue(value)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter is greater than or equal to the specified value.
        /// </summary>
        public static Rule GreaterThanOrEqual<T>(string parameterName, T value, string? description = null) where T : struct
            => new Rule
            {
                Description = description ?? $"{parameterName} >= {value}",
                Expression = $"{parameterName} >= {FormatValue(value)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter is less than the specified value.
        /// </summary>
        public static Rule LessThan<T>(string parameterName, T value, string? description = null) where T : struct
            => new Rule
            {
                Description = description ?? $"{parameterName} < {value}",
                Expression = $"{parameterName} < {FormatValue(value)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter is less than or equal to the specified value.
        /// </summary>
        public static Rule LessThanOrEqual<T>(string parameterName, T value, string? description = null) where T : struct
            => new Rule
            {
                Description = description ?? $"{parameterName} <= {value}",
                Expression = $"{parameterName} <= {FormatValue(value)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter equals the specified value.
        /// </summary>
        public static Rule Equals<T>(string parameterName, T value, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} == {value}",
                Expression = $"{parameterName} == {FormatValue(value)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter does not equal the specified value.
        /// </summary>
        public static Rule NotEquals<T>(string parameterName, T value, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} != {value}",
                Expression = $"{parameterName} != {FormatValue(value)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter is within the specified inclusive range.
        /// </summary>
        public static Rule InRange<T>(string parameterName, T min, T max, string? description = null) where T : struct
            => new Rule
            {
                Description = description ?? $"{parameterName} in range [{min}, {max}]",
                Expression = $"{parameterName} >= {FormatValue(min)} && {parameterName} <= {FormatValue(max)}"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter is outside the specified exclusive range.
        /// </summary>
        public static Rule NotInRange<T>(string parameterName, T min, T max, string? description = null) where T : struct
            => new Rule
            {
                Description = description ?? $"{parameterName} outside range ({min}, {max})",
                Expression = $"{parameterName} < {FormatValue(min)} || {parameterName} > {FormatValue(max)}"
            };

        // ==================== STRING ====================

        /// <summary>
        /// Creates a rule that passes when the string parameter matches the regex pattern.
        /// </summary>
        public static Rule MatchesRegex(string parameterName, string pattern, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} matches pattern",
                // Verbatim string: backslashes are literal, only quotes need escaping ("")
                Expression = $"System.Text.RegularExpressions.Regex.IsMatch({parameterName}, @\"{pattern.Replace("\"", "\"\"")}\")"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter contains the specified value.
        /// </summary>
        public static Rule Contains(string parameterName, string value, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} contains '{value}'",
                Expression = $"{parameterName}.Contains(\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\")"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter starts with the specified value.
        /// </summary>
        public static Rule StartsWith(string parameterName, string value, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} starts with '{value}'",
                Expression = $"{parameterName}.StartsWith(\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\")"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter ends with the specified value.
        /// </summary>
        public static Rule EndsWith(string parameterName, string value, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} ends with '{value}'",
                Expression = $"{parameterName}.EndsWith(\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\")"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter has the exact specified length.
        /// </summary>
        public static Rule HasLength(string parameterName, int length, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} has length {length}",
                Expression = $"{parameterName}.Length == {length}"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter has length greater than or equal to minLength.
        /// </summary>
        public static Rule HasMinLength(string parameterName, int minLength, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} has min length {minLength}",
                Expression = $"{parameterName}.Length >= {minLength}"
            };

        /// <summary>
        /// Creates a rule that passes when the string parameter has length less than or equal to maxLength.
        /// </summary>
        public static Rule HasMaxLength(string parameterName, int maxLength, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} has max length {maxLength}",
                Expression = $"{parameterName}.Length <= {maxLength}"
            };

        // ==================== COLLECTION ====================

        /// <summary>
        /// Creates a rule that passes when the collection has exactly the specified count.
        /// </summary>
        public static Rule CountEquals(string parameterName, int count, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} count == {count}",
                Expression = $"{parameterName}.Count() == {count}"
            };

        /// <summary>
        /// Creates a rule that passes when the collection count is greater than the specified value.
        /// </summary>
        public static Rule CountGreaterThan(string parameterName, int count, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} count > {count}",
                Expression = $"{parameterName}.Count() > {count}"
            };

        /// <summary>
        /// Creates a rule that passes when the collection count is less than the specified value.
        /// </summary>
        public static Rule CountLessThan(string parameterName, int count, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} count < {count}",
                Expression = $"{parameterName}.Count() < {count}"
            };

        /// <summary>
        /// Creates a rule that passes when the collection contains the specified element.
        /// </summary>
        public static Rule Contains<T>(string parameterName, T value, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} contains {value}",
                Expression = $"{parameterName}.Contains({FormatValue(value)})"
            };

        // ==================== BOOLEAN / TYPE ====================

        /// <summary>
        /// Creates a rule that passes when the boolean parameter is true.
        /// </summary>
        public static Rule IsTrue(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is true",
                Expression = $"{parameterName} == true"
            };

        /// <summary>
        /// Creates a rule that passes when the boolean parameter is false.
        /// </summary>
        public static Rule IsFalse(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is false",
                Expression = $"{parameterName} == false"
            };

        /// <summary>
        /// Creates a rule that passes when the parameter is of the specified type.
        /// </summary>
        public static Rule IsOfType<T>(string parameterName, string? description = null)
            => new Rule
            {
                Description = description ?? $"{parameterName} is {typeof(T).Name}",
                Expression = $"{parameterName} is {typeof(T).FullName}"
            };

        // ==================== HELPER ====================

        /// <summary>
        /// Formats a value for use in a C# expression string.
        /// </summary>
        private static string FormatValue<T>(T value)
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

            if (value is Guid g)
                return $"Guid.Parse(\"{g:D}\")";

            if (value is Enum e)
                return $"{value.GetType().FullName}.{e}";

            // For numeric types and everything else, use invariant culture ToString
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null";
        }

        /// <summary>
        /// Validates that a parameter name is safe for use in generated C# code.
        /// Accepts dotted member-access paths (e.g., "customer.Name") but rejects
        /// names that would produce invalid C# identifiers.
        /// </summary>
        private static void ValidateParameterName(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
                throw new ArgumentException("Parameter name cannot be null or empty.", nameof(parameterName));

            // Dotted paths like "order.CustomerId" are valid C# member access
            // Each segment must be a valid identifier
            var segments = parameterName.Split('.');
            foreach (var segment in segments)
            {
                if (string.IsNullOrEmpty(segment))
                    throw new ArgumentException(
                        $"Parameter name '{parameterName}' contains empty segments. " +
                        "Dotted paths must not have consecutive dots or leading/trailing dots.",
                        nameof(parameterName));

                if (!IsValidIdentifier(segment))
                    throw new ArgumentException(
                        $"Parameter name segment '{segment}' in '{parameterName}' is not a valid C# identifier. " +
                        "Identifiers must start with a letter or underscore and contain only letters, digits, or underscores.",
                        nameof(parameterName));
            }
        }

        /// <summary>
        /// Checks if a string is a valid C# identifier.
        /// </summary>
        private static bool IsValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            // First character: letter or underscore
            if (!char.IsLetter(name[0]) && name[0] != '_')
                return false;

            // Remaining characters: letter, digit, or underscore
            for (int i = 1; i < name.Length; i++)
            {
                if (!char.IsLetterOrDigit(name[i]) && name[i] != '_')
                    return false;
            }

            return true;
        }
    }
}
