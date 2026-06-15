using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Models
{
    /// <summary>
    /// Immutable result returned from a single rule evaluation.
    /// Includes detailed information about which rule ran, why it failed,
    /// and results from child rules for full traceability.
    /// </summary>
    /// <remarks>
    /// Uses a readonly struct to avoid per-result heap allocation.
    /// Stored in generic collections (List&lt;T&gt;, Dictionary&lt;K,V&gt;) which
    /// hold structs inline without boxing. Interface access (IReadOnlyList,
    /// IEnumerable) does not box the values themselves in modern .NET.
    /// Benchmarks show struct is competitive with class for large hierarchies
    /// and avoids GC pressure for high-throughput scenarios.
    /// </remarks>
    public readonly record struct RuleResult
    {
        /// <summary>
        /// True if the rule passed all evaluations (expression and children).
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Unique identifier of the rule that produced this result.
        /// </summary>
        public Guid RuleId { get; }

        /// <summary>
        /// Human-readable description of the rule.
        /// </summary>
        public string RuleDescription { get; }

        /// <summary>
        /// Whether the rule was active when evaluated.
        /// Inactive rules return Success=true with IsActive=false.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Optional return value from the rule&apos;s Action delegate.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Exception that caused the failure, if any.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Results from child rules, in evaluation order.
        /// Empty if the rule has no children.
        /// </summary>
        public IReadOnlyList<RuleResult> ChildResults { get; }

        /// <summary>
        /// Initializes a new rule result with full details.
        /// </summary>
        public RuleResult(
            bool Success,
            Guid RuleId = default,
            string RuleDescription = "",
            bool IsActive = true,
            object? Value = null,
            Exception? Exception = null,
            IReadOnlyList<RuleResult>? ChildResults = null)
        {
            this.Success = Success;
            this.RuleId = RuleId;
            this.RuleDescription = RuleDescription;
            this.IsActive = IsActive;
            this.Value = Value;
            this.Exception = Exception;
            this.ChildResults = ChildResults ?? Array.Empty<RuleResult>();
        }

        /// <summary>
        /// Returns the first failing child result, or null if all passed.
        /// Useful for identifying exactly which child rule caused a parent failure.
        /// </summary>
        public RuleResult? FirstFailure =>
            ChildResults.FirstOrDefault(r => !r.Success);

        /// <summary>
        /// Returns all failing child results.
        /// </summary>
        public IEnumerable<RuleResult> AllFailures =>
            ChildResults.Where(r => !r.Success);
    }
}
