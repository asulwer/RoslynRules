using RoslynRules.Abstractions;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynRules.Batch
{
    /// <summary>
    /// A batch of rules compiled and evaluated together as a unit.
    /// Useful when evaluating 10+ rules against the same input with shared context.
    /// </summary>
    public sealed class RuleBatch : IRuleEngine
    {
        private readonly List<Rule> _rules = new List<Rule>();
        private readonly ExpressionCompiler _compiler = new ExpressionCompiler();
        private bool _isCompiled;

        /// <summary>
        /// Rules in this batch.
        /// </summary>
        public IReadOnlyList<Rule> Rules => _rules;

        /// <summary>
        /// Adds a rule to the batch.
        /// </summary>
        public RuleBatch AddRule(Rule rule)
        {
            if (_isCompiled)
                throw new InvalidOperationException("Cannot add rules after compilation.");

            _rules.Add(rule);
            return this;
        }

        /// <summary>
        /// Adds multiple rules to the batch.
        /// </summary>
        public RuleBatch AddRules(IEnumerable<Rule> rules)
        {
            foreach (var rule in rules)
                AddRule(rule);
            return this;
        }

        /// <summary>
        /// Validates all rules in the batch.
        /// </summary>
        public void Validate()
        {
            var errors = ValidateAll();
            if (errors.Any())
            {
                var first = errors[0];
                switch (first.ErrorType)
                {
                    case ValidationErrorType.NoActiveRules:
                        throw new WorkflowException(first.Message);
                    case ValidationErrorType.DuplicateRuleId:
                        throw new DuplicateRuleIdException(errors.Where(e => e.ErrorType == ValidationErrorType.DuplicateRuleId).Select(e => e.EntityId!.Value).ToArray());
                    default:
                        throw new RuleValidationException(first.Message);
                }
            }
        }

        /// <summary>
        /// Validates all rules in the batch, returning all errors found.
        /// Does not throw — returns an empty array if validation succeeds.
        /// </summary>
        /// <returns>Array of validation errors. Empty if valid.</returns>
        public ValidationError[] ValidateAll()
        {
            var errors = new List<ValidationError>();

            if (!_rules.Any())
            {
                errors.Add(new ValidationError(
                    "Batch has no rules.", ValidationErrorType.NoActiveRules));
                return errors.ToArray();
            }

            var ids = _rules.Select(r => r.Id).ToList();
            var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Any())
            {
                foreach (var dupId in duplicates)
                {
                    errors.Add(new ValidationError(
                        $"Duplicate rule ID: {dupId}", ValidationErrorType.DuplicateRuleId, dupId));
                }
            }

            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                errors.AddRange(rule.ValidateAll());
            }

            return errors.ToArray();
        }

        /// <summary>
        /// Compiles all rules in the batch using a shared ExpressionCompiler.
        /// Single compile pass for all rules.
        /// </summary>
        public void Compile(RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            Validate();

            foreach (var rule in _rules.Where(r => r.IsActive))
            {
                rule.Compile(_compiler, parameters, additionalNamespaces, referenceProvider);
            }

            _isCompiled = true;
        }

        /// <summary>
        /// Evaluates all active rules sequentially.
        /// </summary>
        public IEnumerable<RuleResult> Evaluate(params RuleParameter[] parameters)
        {
            EnsureCompiled();

            foreach (var rule in _rules.Where(r => r.IsActive).OrderByDescending(r => r.Priority))
            {
                yield return rule.Execute(parameters);
            }
        }

        /// <summary>
        /// Evaluates all active rules in parallel.
        /// </summary>
        public RuleResult[] EvaluateParallel(params RuleParameter[] parameters)
        {
            EnsureCompiled();

            var activeRules = _rules.Where(r => r.IsActive).OrderByDescending(r => r.Priority).ToArray();
            var results = new RuleResult[activeRules.Length];

            System.Threading.Tasks.Parallel.For(0, activeRules.Length, i =>
            {
                results[i] = activeRules[i].Execute(parameters);
            });

            return results;
        }

        /// <summary>
        /// Evaluates all active rules asynchronously with cancellation support.
        /// Yields results as they are produced for memory-efficient processing of large rule sets.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Token to cancel the stream.</param>
        public async IAsyncEnumerable<RuleResult> EvaluateAsync(
            RuleParameter[] parameters,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            EnsureCompiled();

            foreach (var rule in _rules.Where(r => r.IsActive).OrderByDescending(r => r.Priority))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return await rule.ExecuteAsync(parameters);
            }
        }

        /// <summary>
        /// Evaluates all active rules in parallel asynchronously with cancellation support.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Token to cancel the parallel execution.</param>
        public async Task<RuleResult[]> EvaluateParallelAsync(
            RuleParameter[] parameters,
            CancellationToken cancellationToken = default)
        {
            EnsureCompiled();

            var activeRules = _rules.Where(r => r.IsActive).OrderByDescending(r => r.Priority).ToArray();
            var tasks = activeRules.Select(rule => rule.ExecuteAsync(parameters)).ToArray();

            if (cancellationToken == default || !cancellationToken.CanBeCanceled)
                return await Task.WhenAll(tasks);

            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            var tcs = new TaskCompletionSource<RuleResult[]>();
            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            var whenAll = Task.WhenAll(tasks);
            var completed = await Task.WhenAny(whenAll, tcs.Task);
            return await completed;
        }

        private void EnsureCompiled()
        {
            if (!_isCompiled)
                throw new NotCompiledException(Guid.Empty);
        }

        // ==================== IRuleEngine Aliases ====================

        /// <summary>
        /// Alias for <see cref="Evaluate"/>. Implements <see cref="IRuleEngine"/>.
        /// </summary>
        IEnumerable<RuleResult> IRuleEngine.Execute(params RuleParameter[] parameters) => Evaluate(parameters);

        /// <summary>
        /// Alias for <see cref="EvaluateAsync"/>. Implements <see cref="IRuleEngine"/>.
        /// </summary>
        IAsyncEnumerable<RuleResult> IRuleEngine.ExecuteAsync(RuleParameter[] parameters, CancellationToken cancellationToken) => EvaluateAsync(parameters, cancellationToken);

        /// <summary>
        /// Alias for <see cref="EvaluateParallel"/>. Implements <see cref="IRuleEngine"/>.
        /// </summary>
        RuleResult[] IRuleEngine.ExecuteParallel(params RuleParameter[] parameters) => EvaluateParallel(parameters);

        /// <summary>
        /// Alias for <see cref="EvaluateParallelAsync"/>. Implements <see cref="IRuleEngine"/>.
        /// </summary>
        Task<RuleResult[]> IRuleEngine.ExecuteParallelAsync(RuleParameter[] parameters, CancellationToken cancellationToken) => EvaluateParallelAsync(parameters, cancellationToken);
    }
}
