using System;

namespace RoslynRules.Exceptions
{
    /// <summary>
    /// Base exception for all Rules engine errors.
    /// Catch this to handle any rule-related failure.
    /// </summary>
    public abstract class RulesException : Exception
    {
        protected RulesException(string message) : base(message) { }
        protected RulesException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Thrown when rule structure or syntax validation fails.
    /// Call Validate() before Compile() to catch these early.
    /// </summary>
    public class RuleValidationException : RulesException
    {
        public RuleValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Thrown when a circular reference is detected in the child rule tree.
    /// A rule cannot reference itself or an ancestor as a child.
    /// </summary>
    public class CircularReferenceException : RuleValidationException
    {
        public Guid RuleId { get; }
        public string RuleDescription { get; }

        public CircularReferenceException(Guid ruleId, string description)
            : base($"Circular child rule reference detected at rule '{description}' (Id: {ruleId}). A rule cannot contain itself or an ancestor as a child.")
        {
            RuleId = ruleId;
            RuleDescription = description;
        }
    }

    /// <summary>
    /// Thrown when a C# expression contains syntax errors.
    /// </summary>
    public class SyntaxErrorException : RuleValidationException
    {
        public string Expression { get; }
        public string[] Errors { get; }

        public SyntaxErrorException(string expression, string[] errors)
            : base($"Syntax error in expression: {string.Join("; ", errors)}")
        {
            Expression = expression;
            Errors = errors;
        }
    }

    /// <summary>
    /// Thrown when rule compilation fails (Roslyn compilation errors).
    /// </summary>
    public class RuleCompilationException : RulesException
    {
        public RuleCompilationException(string message) : base(message) { }
        public RuleCompilationException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Thrown when attempting to execute a rule that has not been compiled.
    /// </summary>
    public class NotCompiledException : RuleCompilationException
    {
        public Guid RuleId { get; }

        public NotCompiledException(Guid ruleId)
            : base($"Rule (Id: {ruleId}) must be compiled before execution or have ChildRules defined.")
        {
            RuleId = ruleId;
        }
    }

    /// <summary>
    /// Thrown when a rule exceeds its configured timeout during execution.
    /// </summary>
    public class RuleTimeoutException : RulesException
    {
        public Guid RuleId { get; }
        public TimeSpan Timeout { get; }

        public RuleTimeoutException(Guid ruleId, TimeSpan timeout)
            : base($"Rule (Id: {ruleId}) exceeded timeout of {timeout.TotalSeconds}s.")
        {
            RuleId = ruleId;
            Timeout = timeout;
        }
    }

    /// <summary>
    /// Thrown when a workflow is invalid (empty, duplicate IDs, etc.).
    /// </summary>
    public class WorkflowException : RulesException
    {
        public WorkflowException(string message) : base(message) { }
    }

    /// <summary>
    /// Thrown when a workflow contains duplicate rule IDs.
    /// </summary>
    public class DuplicateRuleIdException : WorkflowException
    {
        public Guid[] DuplicateIds { get; }

        public DuplicateRuleIdException(Guid[] duplicateIds)
            : base($"Workflow contains duplicate rule IDs: {string.Join(", ", duplicateIds)}")
        {
            DuplicateIds = duplicateIds;
        }
    }

    /// <summary>
    /// Thrown when a JIT-only API is called in an AOT/trimming environment.
    /// Use snapshots (JsonSnapshotSerializer / XmlSnapshotSerializer) for AOT-safe rule loading.
    /// </summary>
    public class AotCompatibilityException : RulesException
    {
        public string ApiName { get; }

        public AotCompatibilityException(string apiName)
            : base($"'{apiName}' is not available in AOT/trimming mode. " +
                   "Compile rules in a JIT environment, create snapshots with SnapshotManager, " +
                   "then load and execute those snapshots in AOT. See docs/snapshots.md for the two-stage deployment pattern.")
        {
            ApiName = apiName;
        }
    }
}
