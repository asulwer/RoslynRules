using System;

namespace RoslynRules.Models
{
    /// <summary>
    /// Lightweight parameter container used to pass typed values into rule expressions during compilation and execution.
    /// Replaces DynamicExpresso.Parameter with a minimal, own-implementation alternative.
    /// 
    /// Compile-time: only Name and Type matter. Value can be null.
    /// Execute-time: Name, Type, and Value are all used. Type must match the compile-time type.
    /// </summary>
    public class RuleParameter
    {
        /// <summary>
        /// The parameter name as it appears in the expression string.
        /// Required for both compilation (variable resolution) and execution (not used at runtime but kept for consistency).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The CLR type of the parameter.
        /// Required for both compilation (delegate signature) and execution (type validation).
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The runtime value supplied during execution.
        /// Optional at compile-time (can be null). Required at execution-time.
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// True if this parameter has a runtime value set.
        /// </summary>
        public bool HasValue => Value != null;

        /// <summary>
        /// Initializes a new parameter definition.
        /// </summary>
        /// <param name="name">Parameter name matching the expression identifier.</param>
        /// <param name="type">CLR type of the parameter.</param>
        /// <param name="value">Optional runtime value. Can be null for compilation-only parameters.</param>
        public RuleParameter(string name, Type type, object? value = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value;
        }

        /// <summary>
        /// Returns a compile-time parameter (no value) for use with Compile().
        /// </summary>
        public static RuleParameter ForCompile(string name, Type type) => new(name, type);

        /// <summary>
        /// Returns an execution-time parameter (with value) for use with Execute().
        /// </summary>
        public static RuleParameter ForExecute(string name, Type type, object value) => new(name, type, value ?? throw new ArgumentNullException(nameof(value)));
    }
}
