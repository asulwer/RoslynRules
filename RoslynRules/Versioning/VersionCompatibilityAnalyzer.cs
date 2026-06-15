using RoslynRules.Exceptions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Versioning
{
    /// <summary>
    /// Analyzes version compatibility between rules and workflows.
    /// Helps identify breaking changes and migration requirements.
    /// </summary>
    public static class VersionCompatibilityAnalyzer
    {
        /// <summary>
        /// Analyzes compatibility between two workflow versions.
        /// </summary>
        /// <param name="current">The current/working workflow.</param>
        /// <param name="target">The target workflow to compare against.</param>
        /// <returns>Compatibility analysis result.</returns>
        public static CompatibilityAnalysis Analyze(Workflow current, Workflow target)
        {
            var ruleChanges = new List<RuleVersionChange>();
            var currentVersions = current.GetRuleVersions();
            var targetVersions = target.GetRuleVersions();

            // Check for added rules (in target but not in current)
            foreach (var (ruleId, targetVersion) in targetVersions)
            {
                if (!currentVersions.ContainsKey(ruleId))
                {
                    ruleChanges.Add(new RuleVersionChange(ruleId, null, targetVersion, ChangeType.Added));
                }
            }

            // Check for removed rules (in current but not in target)
            foreach (var (ruleId, currentVersion) in currentVersions)
            {
                if (!targetVersions.ContainsKey(ruleId))
                {
                    ruleChanges.Add(new RuleVersionChange(ruleId, currentVersion, null, ChangeType.Removed));
                }
            }

            // Check for modified rules (different versions)
            foreach (var (ruleId, currentVersion) in currentVersions)
            {
                if (targetVersions.TryGetValue(ruleId, out var targetVersion))
                {
                    if (currentVersion != targetVersion)
                    {
                        var changeType = DetermineChangeType(currentVersion, targetVersion);
                        ruleChanges.Add(new RuleVersionChange(ruleId, currentVersion, targetVersion, changeType));
                    }
                }
            }

            var workflowChanged = current.Version != target.Version;
            var breakingChanges = ruleChanges.Any(c => c.Type == ChangeType.Breaking || c.Type == ChangeType.MajorBump);
            var requiresMigration = breakingChanges || workflowChanged;

            return new CompatibilityAnalysis(
                current.Version,
                target.Version,
                ruleChanges.AsReadOnly(),
                breakingChanges,
                requiresMigration);
        }

        /// <summary>
        /// Validates that all rules in the current workflow are compatible with the target workflow.
        /// Throws if incompatible versions are found.
        /// </summary>
        /// <param name="current">The current workflow.</param>
        /// <param name="target">The target workflow to validate against.</param>
        /// <exception cref="RuleValidationException">Thrown when incompatible versions are detected.</exception>
        public static void ValidateCompatible(Workflow current, Workflow target)
        {
            var analysis = Analyze(current, target);
            if (!analysis.IsCompatible)
            {
                var breaking = analysis.Changes.Where(c => c.Type == ChangeType.Breaking || c.Type == ChangeType.MajorBump).ToList();
                var messages = breaking.Select(c =>
                    $"Rule {c.RuleId}: {c.OldVersion?.ToString() ?? "missing"} -> {c.NewVersion?.ToString() ?? "removed"} ({c.Type})");
                throw new RuleValidationException(
                    $"Workflow version {target.Version} is incompatible with current {current.Version}. " +
                    $"Breaking changes found: {string.Join(", ", messages)}");
            }
        }

        /// <summary>
        /// Determines the type of change between two versions.
        /// </summary>
        private static ChangeType DetermineChangeType(RuleVersion oldVersion, RuleVersion newVersion)
        {
            if (newVersion.Major > oldVersion.Major)
                return ChangeType.MajorBump;
            if (newVersion.Major < oldVersion.Major)
                return ChangeType.Breaking;
            if (newVersion.Minor > oldVersion.Minor)
                return ChangeType.MinorBump;
            if (newVersion.Patch > oldVersion.Patch)
                return ChangeType.PatchBump;
            return ChangeType.Unknown;
        }
    }

    /// <summary>
    /// Result of a compatibility analysis between two workflows.
    /// </summary>
    public readonly record struct CompatibilityAnalysis(
        RuleVersion CurrentWorkflowVersion,
        RuleVersion TargetWorkflowVersion,
        IReadOnlyList<RuleVersionChange> Changes,
        bool HasBreakingChanges,
        bool RequiresMigration)
    {
        /// <summary>
        /// True if the target workflow is backward compatible with the current workflow.
        /// </summary>
        public bool IsCompatible => !HasBreakingChanges && TargetWorkflowVersion.IsCompatibleWith(CurrentWorkflowVersion);
    }

    /// <summary>
    /// Describes a version change for a single rule.
    /// </summary>
    public readonly record struct RuleVersionChange(
        Guid RuleId,
        RuleVersion? OldVersion,
        RuleVersion? NewVersion,
        ChangeType Type);

    /// <summary>
    /// Types of changes that can occur between versions.
    /// </summary>
    public enum ChangeType
    {
        /// <summary>New rule added.</summary>
        Added,
        /// <summary>Rule removed.</summary>
        Removed,
        /// <summary>Major version bumped (breaking changes).</summary>
        MajorBump,
        /// <summary>Minor version bumped (new features, backward compatible).</summary>
        MinorBump,
        /// <summary>Patch version bumped (bug fixes, backward compatible).</summary>
        PatchBump,
        /// <summary>Version decreased (breaking/rollback).</summary>
        Breaking,
        /// <summary>Unknown change type.</summary>
        Unknown
    }
}
