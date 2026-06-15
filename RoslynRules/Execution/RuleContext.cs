using RoslynRules.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RoslynRules.Execution
{
    /// <summary>
    /// Provides thread-safe access to the results of previously executed rules within a workflow.
    /// Used when rules depend on each other (DependsOnRuleId) to share outputs.
    /// </summary>
    public class RuleContext
    {
        private readonly ConcurrentDictionary<Guid, RuleResult> _results = new ConcurrentDictionary<Guid, RuleResult>();

        /// <summary>
        /// Stores the result of a rule execution.
        /// Thread-safe. Called by the workflow engine after each rule completes.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <param name="result">The execution result.</param>
        public void StoreResult(Guid ruleId, RuleResult result)
        {
            _results[ruleId] = result;
        }

        /// <summary>
        /// Retrieves the result of a previously executed rule.
        /// Returns null if the rule has not been executed yet.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <returns>The rule result, or null if not found.</returns>
        public RuleResult? GetResult(Guid ruleId)
        {
            _results.TryGetValue(ruleId, out var result);
            return result;
        }

        /// <summary>
        /// Checks if a rule has been executed and its result is available.
        /// </summary>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <returns>True if the result is available.</returns>
        public bool HasResult(Guid ruleId) => _results.ContainsKey(ruleId);

        /// <summary>
        /// Gets the typed value from a rule's result.
        /// Returns default(T) if the rule is not found, failed, or has no value.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <returns>The typed value, or default(T).</returns>
        public T? GetValue<T>(Guid ruleId)
        {
            if (_results.TryGetValue(ruleId, out var result) && result.Success && result.Value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <summary>
        /// Tries to get the typed value from a rule's result.
        /// Distinguishes between rule not found, rule failed, and successful retrieval.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="ruleId">The unique identifier of the rule.</param>
        /// <param name="value">The typed value if the rule succeeded and the value is of type T.</param>
        /// <returns>True if the rule succeeded and a value of type T was found; otherwise false.</returns>
        public bool TryGetValue<T>(Guid ruleId, [MaybeNullWhen(false)] out T value)
        {
            if (_results.TryGetValue(ruleId, out var result))
            {
                if (result.Success && result.Value is T typedValue)
                {
                    value = typedValue;
                    return true;
                }
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// Gets all stored results.
        /// </summary>
        public IReadOnlyDictionary<Guid, RuleResult> Results => _results;

        /// <summary>
        /// Clears all stored results.
        /// Called by the workflow engine before each workflow execution.
        /// </summary>
        public void Clear() => _results.Clear();
    }
}
