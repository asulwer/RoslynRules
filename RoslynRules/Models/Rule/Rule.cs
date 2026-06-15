using Microsoft.Extensions.Logging;
using RoslynRules.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace RoslynRules.Models
{
    /// <summary>
    /// Represents an individual rule within a workflow.
    /// Each rule evaluates an optional boolean Expression, executes an optional Action,
    /// and can contain child rules that must all succeed for the parent to succeed.
    /// After compilation, rule properties become immutable to ensure thread-safe execution.
    /// Supports both synchronous and asynchronous expressions.
    /// </summary>
    public sealed partial class Rule
    {
        // Compiled delegates wrapped for fast invocation (no DynamicInvoke).
        private CompiledDelegate? _compiledExpression;
        private CompiledDelegate? _compiledAction;
        private bool _isCompiled;

        // Stores the compile-time parameter schemas for validation at execution.
        private RuleParameter[] _compiledParameters = Array.Empty<RuleParameter>();

        // Result cache for memoization.
        private readonly Execution.RuleCache _resultCache = new();

        /// <summary>
        /// Initializes a new rule with default values.
        /// </summary>
        public Rule()
        {
        }

        /// <summary>
        /// Throws if an attempt is made to mutate a property after compilation.
        /// Ensures thread-safe read-only access post-compile.
        /// </summary>
        private void EnsureNotCompiled(string propertyName)
        {
            if (_isCompiled)
                throw new RuleCompilationException($"Cannot modify {propertyName} after rule has been compiled.");
        }

        /// <summary>
        /// Unique identifier for the rule.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Semantic version of this rule. Used for tracking changes and compatibility.
        /// Defaults to 1.0.0 for new rules.
        /// </summary>
        public RuleVersion Version
        {
            get => _version;
            set { EnsureNotCompiled(nameof(Version)); _version = value; }
        }
        private RuleVersion _version = new(1, 0, 0);

        /// <summary>
        /// When this rule was created. Automatically set on first compilation.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// When this rule was last modified. Updated automatically when properties change.
        /// </summary>
        public DateTime ModifiedAt
        {
            get => _modifiedAt;
            set { EnsureNotCompiled(nameof(ModifiedAt)); _modifiedAt = value; }
        }
        private DateTime _modifiedAt = DateTime.UtcNow;

        /// <summary>
        /// Optional identifier of the user/system that last modified this rule.
        /// </summary>
        public string? ModifiedBy
        {
            get => _modifiedBy;
            set { EnsureNotCompiled(nameof(ModifiedBy)); _modifiedBy = value; }
        }
        private string? _modifiedBy;

        /// <summary>
        /// Public constructor for external factory methods.
        /// Preserves the specified ID without generating a new one.
        /// </summary>
        public Rule(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Creates a new rule with the specified version.
        /// </summary>
        public Rule(Guid id, RuleVersion version)
        {
            Id = id;
            _version = version;
        }

        /// <summary>
        /// Bumps the major version (resetting minor and patch to 0).
        /// Use for breaking changes.
        /// </summary>
        public void BumpMajorVersion(string? modifiedBy = null)
        {
            EnsureNotCompiled(nameof(Version));
            _version = _version.IncrementMajor();
            _modifiedAt = DateTime.UtcNow;
            _modifiedBy = modifiedBy;
        }

        /// <summary>
        /// Bumps the minor version (resetting patch to 0).
        /// Use for new features (backward compatible).
        /// </summary>
        public void BumpMinorVersion(string? modifiedBy = null)
        {
            EnsureNotCompiled(nameof(Version));
            _version = _version.IncrementMinor();
            _modifiedAt = DateTime.UtcNow;
            _modifiedBy = modifiedBy;
        }

        /// <summary>
        /// Bumps the patch version.
        /// Use for bug fixes (backward compatible).
        /// </summary>
        public void BumpPatchVersion(string? modifiedBy = null)
        {
            EnsureNotCompiled(nameof(Version));
            _version = _version.IncrementPatch();
            _modifiedAt = DateTime.UtcNow;
            _modifiedBy = modifiedBy;
        }

        /// <summary>
        /// Human-readable description of the rule's purpose.
        /// </summary>
        public string Description 
        { 
            get => _description;
            set { EnsureNotCompiled(nameof(Description)); _description = value; }
        }
        private string _description = string.Empty;

        /// <summary>
        /// Localization key for the rule description. When set, <see cref="GetLocalizedDescription"/>
        /// uses <see cref="IRuleDescriptionProvider"/> to resolve the key to a localized string.
        /// Falls back to <see cref="Description"/> when null or when no provider is available.
        /// </summary>
        public string? DescriptionKey
        {
            get => _descriptionKey;
            set { EnsureNotCompiled(nameof(DescriptionKey)); _descriptionKey = value; }
        }
        private string? _descriptionKey;

        /// <summary>
        /// Optional description provider for localization. Set this to enable
        /// <see cref="GetLocalizedDescription"/> to resolve <see cref="DescriptionKey"/>.
        /// </summary>
        public IRuleDescriptionProvider? DescriptionProvider { get; set; }

        /// <summary>
        /// When false, the rule is skipped during execution.
        /// </summary>
        public bool IsActive 
        { 
            get => _isActive;
            set { EnsureNotCompiled(nameof(IsActive)); _isActive = value; }
        }
        private bool _isActive = true;

        /// <summary>
        /// Execution priority. Higher values execute first.
        /// Default is 0. Negative values execute after default priority rules.
        /// </summary>
        public int Priority
        {
            get => _priority;
            set { EnsureNotCompiled(nameof(Priority)); _priority = value; }
        }
        private int _priority = 0;

        /// <summary>
        /// C# boolean expression evaluated during execution.
        /// Can contain async code (await) if the expression is marked async.
        /// If empty, the expression is treated as passing.
        /// </summary>
        public string Expression 
        { 
            get => _expression;
            set { EnsureNotCompiled(nameof(Expression)); _expression = value; }
        }
        private string _expression = string.Empty;

        /// <summary>
        /// C# expression executed as an action when the rule succeeds.
        /// Can contain async code (await) if the expression is marked async.
        /// If empty, no action is performed.
        /// </summary>
        public string Action 
        { 
            get => _action;
            set { EnsureNotCompiled(nameof(Action)); _action = value; }
        }
        private string _action = string.Empty;

        /// <summary>
        /// Foreign key referencing the parent workflow.
        /// </summary>
        public Guid? WorkflowId 
        { 
            get => _workflowId;
            set { EnsureNotCompiled(nameof(WorkflowId)); _workflowId = value; }
        }
        private Guid? _workflowId;

        /// <summary>
        /// Navigation property to the parent workflow.
        /// </summary>
        public Workflow? Workflow 
        { 
            get => _workflow;
            set { EnsureNotCompiled(nameof(Workflow)); _workflow = value; }
        }
        private Workflow? _workflow;
        
        /// <summary>
        /// Foreign key referencing the parent rule.
        /// </summary>
        public Guid? ParentRuleId 
        { 
            get => _parentRuleId;
            set { EnsureNotCompiled(nameof(ParentRuleId)); _parentRuleId = value; }
        }
        private Guid? _parentRuleId;

        /// <summary>
        /// Maximum time allowed for rule execution. Null means no timeout.
        /// Applies to both Expression and Action execution.
        /// </summary>
        public TimeSpan? Timeout
        {
            get => _timeout;
            set { EnsureNotCompiled(nameof(Timeout)); _timeout = value; }
        }
        private TimeSpan? _timeout;

        /// <summary>
        /// Duration to cache rule evaluation results. Null means no caching.
        /// When set, repeated executions with identical parameters return cached results.
        /// Child rules are evaluated independently; only this rule's final result is cached.
        /// </summary>
        public TimeSpan? CacheDuration
        {
            get => _cacheDuration;
            set { EnsureNotCompiled(nameof(CacheDuration)); _cacheDuration = value; }
        }
        private TimeSpan? _cacheDuration;

        /// <summary>
        /// Navigation property to the parent rule.
        /// </summary>
        public Rule? ParentRule 
        { 
            get => _parentRule;
            set { EnsureNotCompiled(nameof(ParentRule)); _parentRule = value; }
        }
        private Rule? _parentRule;

        /// <summary>
        /// Child rules that must all succeed for this parent rule to succeed.
        /// Evaluated bottom-up before the parent's Expression and Action.
        /// </summary>
        public IList<Rule> ChildRules 
        { 
            get => _childRules;
            set { EnsureNotCompiled(nameof(ChildRules)); _childRules = value; }
        }
        private IList<Rule> _childRules = new List<Rule>();

        /// <summary>
        /// Foreign key referencing another rule that this rule depends on.
        /// When set, the dependency rule's result is made available during execution.
        /// The dependent rule executes after its dependency.
        /// </summary>
        public Guid? DependsOnRuleId
        {
            get => _dependsOnRuleId;
            set { EnsureNotCompiled(nameof(DependsOnRuleId)); _dependsOnRuleId = value; }
        }
        private Guid? _dependsOnRuleId;

        /// <summary>
        /// Navigation property to the rule this rule depends on.
        /// </summary>
        public Rule? DependsOnRule
        {
            get => _dependsOnRule;
            set { EnsureNotCompiled(nameof(DependsOnRule)); _dependsOnRule = value; }
        }
        private Rule? _dependsOnRule;

        /// <summary>
        /// Optional logger for observing rule execution.
        /// Set this to any ILogger implementation (Serilog, NLog, etc.).
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Execution metrics for this rule: eval count, average time, failure rate, last execution.
        /// Updated atomically during execution. Access is thread-safe.
        /// </summary>
        public RuleMetrics Metrics => new RuleMetrics(_metrics.EvalCount, _metrics.FailureCount, _metrics.TotalTicks, _metrics.LastExecuted?.Ticks ?? 0);

        /// <summary>
        /// Clears the cached result for this rule, forcing the next evaluation to re-execute.
        /// Also resets execution metrics.
        /// Thread-safe.
        /// </summary>
        public void ClearCache()
        {
            _resultCache.Clear();
            _metrics.Reset();
        }
        private RuleMetrics _metrics;

        /// <summary>
        /// Returns a localized description of the rule.
        /// If <see cref="DescriptionKey"/> is set and <see cref="DescriptionProvider"/> is available,
        /// resolves the key through the provider. Otherwise falls back to <see cref="Description"/>.
        /// </summary>        /// <param name="culture">Optional culture code (e.g., "en-US", "fr-FR"). Null uses the default culture.</param>
        /// <returns>The localized or default rule description.</returns>
        public string GetLocalizedDescription(string? culture = null)
        {
            if (!string.IsNullOrEmpty(DescriptionKey) && DescriptionProvider != null)
            {
                var localized = DescriptionProvider.GetDescription(DescriptionKey, culture);
                if (!string.IsNullOrEmpty(localized))
                    return localized;
            }

            return Description;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString() => $"Rule: {Description} (Id: {Id})";
    }
}
