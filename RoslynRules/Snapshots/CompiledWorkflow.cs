using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Snapshots;

/// <summary>
/// Represents a fully compiled workflow that can be executed but not modified.
/// Created by compiling a Workflow and capturing all compiled delegates.
/// 
/// JIT: Can create CompiledWorkflow instances via Workflow.Compile().
/// AOT: Can only execute pre-existing CompiledWorkflow instances loaded from snapshots.
/// </summary>
public sealed class CompiledWorkflow
{
    private readonly Workflow _workflow;

    /// <summary>
    /// Creates a CompiledWorkflow from an already-compiled Workflow.
    /// Internal use only; use CompiledWorkflow.Compile() for JIT scenarios.
    /// </summary>
    internal CompiledWorkflow(Workflow workflow)
    {
        if (!workflow.Rules.All(r => IsCompiled(r)))
            throw new InvalidOperationException("All rules must be compiled before creating a CompiledWorkflow.");

        _workflow = workflow;
    }

    /// <summary>
    /// Unique identifier for the compiled workflow.
    /// </summary>
    public Guid Id => _workflow.Id;

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description => _workflow.Description;

    /// <summary>
    /// Semantic version.
    /// </summary>
    public RuleVersion Version => _workflow.Version;

    /// <summary>
    /// When created.
    /// </summary>
    public DateTime CreatedAt => _workflow.CreatedAt;

    /// <summary>
    /// When last modified.
    /// </summary>
    public DateTime ModifiedAt => _workflow.ModifiedAt;

    /// <summary>
    /// Whether the workflow is active.
    /// </summary>
    public bool IsActive => _workflow.IsActive;

    /// <summary>
    /// The underlying workflow (read-only after compilation).
    /// </summary>
    public Workflow Workflow => _workflow;

    /// <summary>
    /// Compiles a Workflow and returns an immutable CompiledWorkflow.
    /// JIT-only: requires Roslyn compilation which uses reflection.
    /// </summary>
    /// <param name="workflow">The workflow to compile.</param>
    /// <param name="parameters">Parameter definitions used for compilation.</param>
    /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
    /// <param name="referenceProvider">Optional custom assembly reference provider.</param>
    public static CompiledWorkflow Compile(Workflow workflow, RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null)
    {
        AotCompatibility.ThrowIfAot(nameof(CompiledWorkflow.Compile));
        workflow.Compile(parameters, additionalNamespaces, referenceProvider);
        return new CompiledWorkflow(workflow);
    }

    /// <summary>
    /// Executes all active rules sequentially.
    /// AOT-safe: only executes pre-compiled delegates.
    /// </summary>
    public IEnumerable<RuleResult> Execute(params RuleParameter[] parameters)
        => _workflow.Execute(parameters);

    /// <summary>
    /// Executes all active rules in parallel.
    /// AOT-safe: only executes pre-compiled delegates.
    /// </summary>
    public RuleResult[] ExecuteParallel(params RuleParameter[] parameters)
        => _workflow.ExecuteParallel(parameters);

    /// <summary>
    /// Executes all active rules asynchronously.
    /// AOT-safe: only executes pre-compiled delegates.
    /// </summary>
    public IAsyncEnumerable<RuleResult> ExecuteAsync(RuleParameter[] parameters, System.Threading.CancellationToken cancellationToken = default)
        => _workflow.ExecuteAsync(parameters, cancellationToken);

    /// <summary>
    /// Executes all active rules in parallel asynchronously.
    /// AOT-safe: only executes pre-compiled delegates.
    /// </summary>
    public System.Threading.Tasks.Task<RuleResult[]> ExecuteParallelAsync(RuleParameter[] parameters, System.Threading.CancellationToken cancellationToken = default)
        => _workflow.ExecuteParallelAsync(parameters, cancellationToken);

    /// <summary>
    /// Validates the compiled workflow.
    /// AOT-safe: performs structural validation only.
    /// </summary>
    public void Validate() => _workflow.Validate();

    /// <summary>
    /// Gets a snapshot of this compiled workflow for serialization.
    /// JIT-only: requires reflection to capture rule state.
    /// AOT: snapshots can only be created by JIT, not consumed.
    /// </summary>
    public WorkflowSnapshot ToSnapshot()
        => WorkflowSnapshot.FromWorkflow(_workflow);

    private static bool IsCompiled(Rule rule)
    {
        // Access private field via reflection since _isCompiled is private
        var field = typeof(Rule).GetField("_isCompiled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            return (bool)field.GetValue(rule)!;

        // Fallback: try to detect compilation by checking if delegate fields are set
        var exprField = typeof(Rule).GetField("_compiledExpression", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return exprField?.GetValue(rule) != null;
    }
}
