using RoslynRules.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynRules.Abstractions
{
    /// <summary>
    /// Core abstraction for a rules engine that compiles and evaluates rules against input parameters.
    /// Implemented by <see cref="Models.Workflow"/> and <see cref="Batch.RuleBatch"/>.
    /// Enables dependency injection, unit testing with mocks, and swapping implementations.
    /// </summary>
    public interface IRuleEngine
    {
        /// <summary>
        /// Validates all rules without compiling them.
        /// Throws a <see cref="Exceptions.RulesException"/> derivative if validation fails.
        /// </summary>
        void Validate();

        /// <summary>
        /// Validates all rules without compiling them, returning all errors found.
        /// Does not throw — returns an empty array if validation succeeds.
        /// </summary>
        /// <returns>Array of validation errors. Empty if valid.</returns>
        ValidationError[] ValidateAll();

        /// <summary>
        /// Compiles all rules for the given parameter signature.
        /// Must be called once before execution.
        /// </summary>
        /// <param name="parameters">Input parameter definitions (name + type).</param>
        /// <param name="additionalNamespaces">Optional extra namespaces for compilation.</param>
        /// <param name="referenceProvider">Optional custom assembly reference provider for sandboxing.</param>
        void Compile(RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null);

        /// <summary>
        /// Executes all rules sequentially against the provided input values.
        /// Must call <see cref="Compile"/> first.
        /// </summary>
        /// <param name="parameters">Runtime parameter values (name + value).</param>
        /// <returns>Results for every executed rule.</returns>
        IEnumerable<RuleResult> Execute(params RuleParameter[] parameters);

        /// <summary>
        /// Executes all rules asynchronously.
        /// For sync rules this is equivalent to <see cref="Execute"/> wrapped in an async stream.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Async stream of results for every executed rule.</returns>
        IAsyncEnumerable<RuleResult> ExecuteAsync(RuleParameter[] parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes independent rules in parallel for maximum throughput.
        /// Rules with dependencies (DependsOnRuleId) still run after their dependency completes.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Results for every executed rule.</returns>
        RuleResult[] ExecuteParallel(params RuleParameter[] parameters);

        /// <summary>
        /// Executes rules in parallel asynchronously.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Results for every executed rule.</returns>
        Task<RuleResult[]> ExecuteParallelAsync(RuleParameter[] parameters, CancellationToken cancellationToken = default);
    }
}
