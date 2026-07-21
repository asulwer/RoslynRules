using RoslynRules.Abstractions;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RoslynRules.Execution;

namespace RoslynRules.Models
{
    /// <summary>
    /// A workflow is a container for a collection of rules that are evaluated together.
    /// Owns an ExpressionCompiler used to compile all contained rules.
    /// Supports both sequential and parallel execution of independent rules.
    /// Supports both synchronous and asynchronous expressions.
    /// Supports rule action chaining via DependsOnRuleId for data-flow dependencies.
    /// </summary>
    public class Workflow : IRuleEngine
    {
        private ExpressionCompiler _compiler = new ExpressionCompiler();
        private List<Rule> _rules = new List<Rule>();
        private bool _isCompiled;

        /// <summary>
        /// Initializes a new workflow with default values.
        /// </summary>
        public Workflow()
        {
        }

        /// <summary>
        /// Unique identifier for the workflow.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Human-readable description of the workflow.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Semantic version of this workflow. Used for tracking changes and compatibility.
        /// Defaults to 1.0.0 for new workflows.
        /// </summary>
        public RuleVersion Version
        {
            get => _version;
            set
            {
                if (_isCompiled)
                    throw new InvalidOperationException("Workflow.Version cannot be modified after compilation.");
                _version = value;
            }
        }
        private RuleVersion _version = new(1, 0, 0);

        /// <summary>
        /// When this workflow was created.
        /// </summary>
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// When this workflow was last modified.
        /// </summary>
        public DateTime ModifiedAt
        {
            get => _modifiedAt;
            set
            {
                if (_isCompiled)
                    throw new InvalidOperationException("Workflow.ModifiedAt cannot be modified after compilation.");
                _modifiedAt = value;
            }
        }
        private DateTime _modifiedAt = DateTime.UtcNow;

        /// <summary>
        /// Optional identifier of the user/system that last modified this workflow.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// When false, the entire workflow and its rules are skipped.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Top-level rules in this workflow. Child rules are nested inside their parents.
        /// Returns IReadOnlyList after compilation to enforce immutability.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when setting Rules after compilation.</exception>
        public IList<Rule> Rules
        {
            get => _isCompiled ? _rules.AsReadOnly() : _rules;
            set
            {
                if (_isCompiled)
                    throw new InvalidOperationException("Workflow.Rules cannot be modified after compilation.");
                _rules = value is List<Rule> list ? list : value?.ToList() ?? new List<Rule>();
            }
        }

        /// <summary>
        /// Checks if all rules in this workflow have compatible versions with the target workflow.
        /// Rules with the same major version and equal or greater minor/patch are compatible.
        /// </summary>
        /// <param name="target">The workflow to compare against.</param>
        /// <returns>True if this workflow's version is compatible with the target.</returns>
        public bool IsVersionCompatibleWith(Workflow target)
        {
            return Version.IsCompatibleWith(target.Version);
        }

        /// <summary>
        /// Gets a dictionary of all rules by their version status.
        /// Useful for migration planning and compatibility analysis.
        /// </summary>
        /// <returns>Dictionary of rule IDs to their versions.</returns>
        public Dictionary<Guid, RuleVersion> GetRuleVersions()
        {
            var result = new Dictionary<Guid, RuleVersion>();
            foreach (var rule in Rules)
            {
                CollectRuleVersions(rule, result);
            }
            return result;
        }

        private static void CollectRuleVersions(Rule rule, Dictionary<Guid, RuleVersion> versions)
        {
            versions[rule.Id] = rule.Version;
            foreach (var child in rule.ChildRules)
            {
                CollectRuleVersions(child, versions);
            }
        }

        /// <summary>
        /// Bumps the major version (resetting minor and patch to 0).
        /// Use for breaking changes.
        /// </summary>
        public void BumpMajorVersion(string? modifiedBy = null)
        {
            if (_isCompiled)
                throw new InvalidOperationException("Workflow version cannot be modified after compilation.");
            _version = _version.IncrementMajor();
            _modifiedAt = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Bumps the minor version (resetting patch to 0).
        /// Use for new features (backward compatible).
        /// </summary>
        public void BumpMinorVersion(string? modifiedBy = null)
        {
            if (_isCompiled)
                throw new InvalidOperationException("Workflow version cannot be modified after compilation.");
            _version = _version.IncrementMinor();
            _modifiedAt = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Bumps the patch version.
        /// Use for bug fixes (backward compatible).
        /// </summary>
        public void BumpPatchVersion(string? modifiedBy = null)
        {
            if (_isCompiled)
                throw new InvalidOperationException("Workflow version cannot be modified after compilation.");
            _version = _version.IncrementPatch();
            _modifiedAt = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }

        /// <summary>
        /// Validates the entire workflow and all contained rules.
        /// Checks workflow consistency and delegates to each rule&apos;s Validate method.
        /// Also validates that dependency chains (DependsOnRuleId) contain no cycles.
        /// Call before Compile to catch errors early.
        /// </summary>
        /// <exception cref="WorkflowException">Thrown when workflow has no active rules.</exception>
        /// <exception cref="RuleValidationException">Thrown when a rule is invalid.</exception>
        /// <exception cref="DuplicateRuleIdException">Thrown when duplicate rule IDs exist.</exception>
        /// <exception cref="CircularReferenceException">Thrown when a dependency cycle is detected.</exception>
        public void Validate()
        {
            var errors = ValidateAll();
            if (errors.Any())
            {
                // Throw the most specific exception type based on the first error
                var first = errors[0];
                switch (first.ErrorType)
                {
                    case ValidationErrorType.CircularReference:
                        throw new CircularReferenceException(first.EntityId!.Value, first.EntityDescription ?? "");
                    case ValidationErrorType.DuplicateRuleId:
                        throw new DuplicateRuleIdException(errors.Where(e => e.ErrorType == ValidationErrorType.DuplicateRuleId).Select(e => e.EntityId!.Value).ToArray());
                    case ValidationErrorType.MissingDependency:
                        throw new RuleValidationException(first.Message);
                    case ValidationErrorType.SyntaxError:
                        throw new SyntaxErrorException("", new[] { first.Message });
                    default:
                        throw new WorkflowException(first.Message);
                }
            }
        }

        /// <summary>
        /// Validates the entire workflow and all contained rules, returning all errors found.
        /// Does not throw — returns an empty array if validation succeeds.
        /// Checks workflow consistency, rule syntax, duplicate IDs, and dependency cycles.
        /// </summary>
        /// <returns>Array of validation errors. Empty if valid.</returns>
        public ValidationError[] ValidateAll()
        {
            var errors = new List<ValidationError>();

            // 1. Workflow must have at least one active rule.
            var activeRules = Rules.Where(r => r.IsActive).ToList();
            if (activeRules.Count == 0)
            {
                errors.Add(new ValidationError(
                    $"Workflow &apos;{Description}&apos; (Id: {Id}) has no active rules.",
                    ValidationErrorType.NoActiveRules, Id, Description));
                return errors.ToArray();
            }

            // 2. Validate each top-level rule, passing available IDs for dependency checks.
            var availableIds = activeRules.Select(r => r.Id).ToList();
            foreach (var rule in activeRules)
            {
                errors.AddRange(rule.ValidateAll(availableIds));
            }

            // 3. Detect duplicate rule IDs within this workflow.
            var ids = activeRules.Select(r => r.Id).ToList();
            var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                foreach (var dupId in duplicates)
                {
                    errors.Add(new ValidationError(
                        $"Duplicate rule ID: {dupId}",
                        ValidationErrorType.DuplicateRuleId, dupId));
                }
            }

            // 4. Validate dependency chains: no cycles, all referenced rules exist.
            ValidateDependencies(errors);

            return errors.ToArray();
        }

        /// <summary>
        /// Validates that all DependsOnRuleId references are valid and form no cycles.
        /// Errors are collected into the provided list instead of thrown.
        /// </summary>
        private void ValidateDependencies(List<ValidationError> errors)
        {
            var activeRules = Rules.Where(r => r.IsActive).ToList();
            var ruleLookup = new Dictionary<Guid, Rule>();
            foreach (var rule in activeRules)
            {
                if (!ruleLookup.ContainsKey(rule.Id))
                {
                    ruleLookup[rule.Id] = rule;
                }
                // Duplicate IDs are reported separately by ValidateAll
            }
            var visited = new HashSet<Guid>();
            var recursionStack = new HashSet<Guid>();

            foreach (var rule in activeRules)
            {
                if (!visited.Contains(rule.Id))
                {
                    try
                    {
                        ValidateDependencyChain(rule, ruleLookup, visited, recursionStack);
                    }
                    catch (CircularReferenceException ex)
                    {
                        errors.Add(new ValidationError(
                            ex.Message, ValidationErrorType.CircularReference, ex.RuleId, ex.RuleDescription));
                    }
                    catch (RuleValidationException ex)
                    {
                        errors.Add(new ValidationError(
                            ex.Message, ValidationErrorType.MissingDependency, rule.Id, rule.Description));
                    }
                }
            }
        }

        /// <summary>
        /// DFS-based cycle detection for dependency chains.
        /// </summary>
        private static void ValidateDependencyChain(Rule rule, Dictionary<Guid, Rule> lookup, HashSet<Guid> visited, HashSet<Guid> recursionStack)
        {
            // Check for cycle first
            if (recursionStack.Contains(rule.Id))
            {
                throw new CircularReferenceException(rule.Id, $"Dependency chain on rule &apos;{rule.Description}&apos;");
            }

            // Already fully processed
            if (!visited.Add(rule.Id))
                return;

            recursionStack.Add(rule.Id);

            if (rule.DependsOnRuleId.HasValue)
            {
                var depId = rule.DependsOnRuleId.Value;
                if (!lookup.ContainsKey(depId))
                {
                    throw new RuleValidationException(
                        $"Rule &apos;{rule.Description}&apos; (Id: {rule.Id}) depends on rule {depId} which does not exist or is inactive.");
                }

                var depRule = lookup[depId];
                ValidateDependencyChain(depRule, lookup, visited, recursionStack);
            }

            recursionStack.Remove(rule.Id);
        }

        // ==================== COMPILATION ====================

        /// <summary>
        /// Compiles all active rules in this workflow using the shared ExpressionCompiler.
        /// After compilation, rule properties become immutable.
        /// Call once after workflow creation or when rules change.
        /// </summary>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        public void Compile(RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            AotCompatibility.ThrowIfAot(nameof(Compile));
            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                rule.Compile(_compiler, parameters, additionalNamespaces, referenceProvider);
            }
            _isCompiled = true;
        }

        // ==================== DEPENDENCY MANAGEMENT ====================

        /// <summary>
        /// Builds a topologically sorted list of rules respecting both Priority and DependsOnRuleId.
        /// Rules with dependencies execute after their dependencies.
        /// Within the same dependency level, higher Priority executes first.
        /// Delegates to GraphAlgorithms.TopologicalSort for the core algorithm.
        /// </summary>
        /// <returns>Rules in execution order.</returns>
        private List<Rule> GetExecutionOrder()
        {
            var activeRules = Rules.Where(r => r.IsActive).ToList();

            // Stable comparer: higher priority first, then preserve original list order
            var indexMap = activeRules.Select((r, i) => new { r.Id, Index = i }).ToDictionary(x => x.Id, x => x.Index);
            var priorityComparer = Comparer<Rule>.Create((a, b) =>
            {
                var priorityCompare = b.Priority.CompareTo(a.Priority);
                if (priorityCompare != 0) return priorityCompare;
                // Stable sort: preserve original list order for equal priorities
                return indexMap[a.Id].CompareTo(indexMap[b.Id]);
            });

            return GraphAlgorithms.TopologicalSort(
                activeRules,
                r => r.Id,
                r => r.DependsOnRuleId,
                priorityComparer);
        }

        /// <summary>
        /// Determines whether a rule's dependency is satisfied for the current execution pass.
        /// A rule is runnable when it has no dependency, its dependency has already executed,
        /// or its dependency is dangling (points outside the active rule set). Dangling
        /// dependencies are tolerated identically across sequential and parallel execution.
        /// </summary>
        private static bool DependencySatisfied(Rule rule, HashSet<Guid> executed, HashSet<Guid> activeIds)
        {
            var dep = rule.DependsOnRuleId;
            return !dep.HasValue || executed.Contains(dep.Value) || !activeIds.Contains(dep.Value);
        }

        // ==================== SYNCHRONOUS EXECUTION ====================

        /// <summary>
        /// Executes all active rules sequentially in dependency order, yielding a RuleResult for each.
        /// Does not short-circuit; every active rule is evaluated.
        /// Rules with DependsOnRuleId execute after their dependencies.
        /// Use this when rules are simple and overhead of parallelism isn&apos;t worth it.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <returns>Enumerable of results, one per evaluated rule.</returns>
        public IEnumerable<RuleResult> Execute(params RuleParameter[] parameters)
        {
            if (!IsActive)
                yield break;

            var context = new RuleContext();
            var orderedRules = GetExecutionOrder();

            foreach (var rule in orderedRules)
            {
                yield return rule.ExecuteWithContext(context, parameters);
            }
        }

        /// <summary>
        /// Executes all active rules in parallel for maximum throughput.
        /// Rules with dependencies are executed in dependency order; independent rules run concurrently.
        /// Results are returned in rule order (sorted by priority, with dependencies before dependents).
        /// Child rules within a parent still execute sequentially (bottom-up dependency).
        /// Use this when rules are complex, numerous, or CPU-intensive.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <returns>Array of results in rule order.</returns>
        public RuleResult[] ExecuteParallel(params RuleParameter[] parameters)
        {
            if (!IsActive)
                return Array.Empty<RuleResult>();

            var orderedRules = GetExecutionOrder();
            if (orderedRules.Count == 0)
                return Array.Empty<RuleResult>();

            var context = new RuleContext();

            // Level-based execution: each pass runs every rule whose dependencies are already
            // satisfied (in parallel), then advances. Collecting all runnable rules per pass —
            // rather than only a consecutive prefix — maximizes parallelism, and dangling
            // dependencies are tolerated the same way sequential Execute tolerates them.
            var activeIds = new HashSet<Guid>(orderedRules.Select(r => r.Id));
            var executed = new HashSet<Guid>();
            var resultById = new Dictionary<Guid, RuleResult>(orderedRules.Count);
            var remaining = new List<Rule>(orderedRules);

            while (remaining.Count > 0)
            {
                var batch = remaining.Where(r => DependencySatisfied(r, executed, activeIds)).ToList();
                if (batch.Count == 0)
                {
                    // Unreachable when GetExecutionOrder succeeds (it throws on cycles first),
                    // but guard against an infinite loop.
                    throw new CircularReferenceException(
                        remaining[0].Id,
                        $"Dependency resolution stalled at rule '{remaining[0].Description}' (Id: {remaining[0].Id}).");
                }

                // Execute batch in parallel
                var batchResults = new RuleResult[batch.Count];
                System.Threading.Tasks.Parallel.For(0, batch.Count, i =>
                {
                    batchResults[i] = batch[i].ExecuteWithContext(context, parameters);
                });

                for (int i = 0; i < batch.Count; i++)
                {
                    resultById[batch[i].Id] = batchResults[i];
                    executed.Add(batch[i].Id);
                }

                remaining.RemoveAll(r => executed.Contains(r.Id));
            }

            // Return in execution order (topological + priority).
            return orderedRules.Select(r => resultById[r.Id]).ToArray();
        }

        // ==================== ASYNCHRONOUS EXECUTION ====================

        /// <summary>
        /// Executes all active rules asynchronously in dependency order, yielding a RuleResult for each.
        /// Supports cancellation to stop mid-stream.
        /// Rules with DependsOnRuleId execute after their dependencies.
        /// Properly awaits async expressions in rules.
        /// Use this when rules contain async I/O (database lookups, HTTP calls)
        /// or when consuming results via await foreach.
        /// </summary>
        /// <param name="parameters">Runtime parameter values passed to each rule.</param>
        /// <param name="cancellationToken">Token to cancel execution mid-stream.</param>
        /// <returns>Enumerable of async results, one per evaluated rule.</returns>
        public async IAsyncEnumerable<RuleResult> ExecuteAsync(
            RuleParameter[] parameters,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                yield break;

            var context = new RuleContext();
            var orderedRules = GetExecutionOrder();

            foreach (var rule in orderedRules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return await rule.ExecuteWithContextAsync(context, parameters);
            }
        }

        /// <summary>
        /// Executes all active rules in parallel asynchronously for maximum throughput.
        /// Independent rules run concurrently; dependent rules execute after their dependencies.
        /// Results are returned in rule order.
        /// Supports cancellation to abort before all rules complete.
        /// Properly awaits async expressions in rules.
        /// Use this for maximum performance with async I/O-bound rules.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Token to cancel the parallel execution.</param>
        /// <returns>Array of results in rule order.</returns>
        public async Task<RuleResult[]> ExecuteParallelAsync(
            RuleParameter[] parameters,
            CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return Array.Empty<RuleResult>();

            var orderedRules = GetExecutionOrder();
            if (orderedRules.Count == 0)
                return Array.Empty<RuleResult>();

            var context = new RuleContext();

            // Level-based execution: each pass runs all rules whose dependencies are satisfied,
            // concurrently. Dangling dependencies are tolerated like sequential Execute.
            var activeIds = new HashSet<Guid>(orderedRules.Select(r => r.Id));
            var executed = new HashSet<Guid>();
            var resultById = new Dictionary<Guid, RuleResult>(orderedRules.Count);
            var remaining = new List<Rule>(orderedRules);

            while (remaining.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = remaining.Where(r => DependencySatisfied(r, executed, activeIds)).ToList();
                if (batch.Count == 0)
                {
                    // Unreachable when GetExecutionOrder succeeds; guard against infinite loop.
                    throw new CircularReferenceException(
                        remaining[0].Id,
                        $"Dependency resolution stalled at rule '{remaining[0].Description}' (Id: {remaining[0].Id}).");
                }

                // Execute this batch in parallel
                var tasks = batch.Select(rule => rule.ExecuteWithContextAsync(context, parameters)).ToArray();
                var results = await Task.WhenAll(tasks);

                for (int i = 0; i < batch.Count; i++)
                {
                    resultById[batch[i].Id] = results[i];
                    executed.Add(batch[i].Id);
                }

                remaining.RemoveAll(r => executed.Contains(r.Id));
            }

            // Return results in original rule order (sorted by priority, with dependencies before dependents)
            var activeRules = Rules.Where(r => r.IsActive).ToArray();
            return activeRules.Select(r => resultById[r.Id]).ToArray();
        }

        /// <summary>
        /// Executes rules in buffered chunks, yielding arrays of results.
        /// Rules with dependencies are executed in dependency order within each batch.
        /// Useful for processing large rule sets in batches rather than one at a time.
        /// Supports cancellation and respects priority ordering within each batch.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="bufferSize">Number of rules to evaluate per batch.</param>
        /// <param name="cancellationToken">Token to cancel the stream.</param>
        /// <returns>IAsyncEnumerable of result arrays, one chunk per yield.</returns>
        public async IAsyncEnumerable<RuleResult[]> ExecuteBufferedAsync(
            RuleParameter[] parameters,
            int bufferSize = 10,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be at least 1.");

            if (!IsActive)
                yield break;

            var context = new RuleContext();
            var orderedRules = GetExecutionOrder();

            // Only rules whose dependencies are already satisfied are placed in a chunk, so a
            // dependent rule is never run in the same parallel buffer as its dependency. Each
            // chunk is capped at bufferSize; remaining runnable rules spill into later chunks.
            var activeIds = new HashSet<Guid>(orderedRules.Select(r => r.Id));
            var executed = new HashSet<Guid>();
            var remaining = new List<Rule>(orderedRules);

            while (remaining.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = remaining.Where(r => DependencySatisfied(r, executed, activeIds)).Take(bufferSize).ToArray();
                if (batch.Length == 0)
                {
                    // Unreachable when GetExecutionOrder succeeds; guard against infinite loop.
                    throw new CircularReferenceException(
                        remaining[0].Id,
                        $"Dependency resolution stalled at rule '{remaining[0].Description}' (Id: {remaining[0].Id}).");
                }

                var tasks = batch.Select(rule => rule.ExecuteWithContextAsync(context, parameters)).ToArray();
                var results = await Task.WhenAll(tasks);

                foreach (var rule in batch)
                    executed.Add(rule.Id);
                remaining.RemoveAll(r => executed.Contains(r.Id));

                yield return results;
            }
        }
    }
}
