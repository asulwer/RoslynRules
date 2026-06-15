using System;
using System.IO;

namespace RoslynRules.Snapshots;

/// <summary>
/// Central manager for creating, loading, and executing snapshots.
/// 
/// JIT Mode: Can create snapshots from compiled workflows and save them.
/// AOT Mode: Can only load and execute pre-existing snapshots.
/// 
/// This design ensures AOT-safe consumption paths are clearly separated
/// from JIT-only creation paths.
/// </summary>
public static class SnapshotManager
{
    // ============ JIT ONLY: Create snapshots ============

    /// <summary>
    /// Creates a snapshot from a compiled workflow.
    /// JIT ONLY: requires reflection access to compiled rule state.
    /// </summary>
    public static WorkflowSnapshot CreateSnapshot(CompiledWorkflow compiledWorkflow)
    {
        return compiledWorkflow.ToSnapshot();
    }

    /// <summary>
    /// Compiles a workflow and immediately creates a snapshot.
    /// JIT ONLY: requires Roslyn compilation.
    /// </summary>
    public static WorkflowSnapshot CompileAndSnapshot(
        Models.Workflow workflow,
        Models.RuleParameter[] parameters,
        string[]? additionalNamespaces = null,
        Compiler.AssemblyReferenceProvider? referenceProvider = null)
    {
        var compiled = CompiledWorkflow.Compile(workflow, parameters, additionalNamespaces, referenceProvider);
        return CreateSnapshot(compiled);
    }

    /// <summary>
    /// Saves a snapshot using the provided serializer.
    /// JIT ONLY: snapshot creation requires JIT.
    /// </summary>
    public static void SaveSnapshot(WorkflowSnapshot snapshot, ISnapshotSerializer serializer, string filePath)
    {
        serializer.SaveWorkflowToFile(snapshot, filePath);
    }

    // ============ AOT Safe: Load and execute snapshots ============

    /// <summary>
    /// Loads a workflow snapshot from a file using the provided serializer.
    /// AOT SAFE: no reflection required for deserialization.
    /// </summary>
    public static WorkflowSnapshot LoadSnapshot(ISnapshotSerializer serializer, string filePath)
    {
        return serializer.LoadWorkflowFromFile(filePath);
    }

    /// <summary>
    /// Loads a workflow snapshot from a string using the provided serializer.
    /// AOT SAFE: no reflection required for deserialization.
    /// </summary>
    public static WorkflowSnapshot LoadSnapshotFromString(ISnapshotSerializer serializer, string data)
    {
        return serializer.DeserializeWorkflow(data);
    }

    /// <summary>
    /// Restores a compiled workflow from a snapshot.
    /// NOTE: The returned CompiledWorkflow is NOT actually compiled yet —
    /// it wraps the restored Workflow but compilation is skipped.
    /// In AOT mode, you cannot compile, so this is for JIT scenarios
    /// where you load a snapshot and then compile.
    /// 
    /// For pure AOT execution, use RestoreForExecution instead.
    /// </summary>
    public static Models.Workflow RestoreWorkflow(WorkflowSnapshot snapshot)
    {
        return snapshot.ToWorkflow();
    }

    /// <summary>
    /// Restores and compiles a workflow from a snapshot.
    /// JIT ONLY: requires Roslyn compilation.
    /// </summary>
    public static CompiledWorkflow RestoreAndCompile(
        WorkflowSnapshot snapshot,
        Models.RuleParameter[] parameters,
        string[]? additionalNamespaces = null,
        Compiler.AssemblyReferenceProvider? referenceProvider = null)
    {
        var workflow = snapshot.ToWorkflow();
        return CompiledWorkflow.Compile(workflow, parameters, additionalNamespaces, referenceProvider);
    }
}
