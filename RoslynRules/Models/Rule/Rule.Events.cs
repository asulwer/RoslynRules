using System;

namespace RoslynRules.Models
{
    public sealed partial class Rule
    {
        // ==================== EVENTS ====================

        /// <summary>
        /// Fired before a rule executes. Set Cancel = true to skip execution.
        /// </summary>
        public event EventHandler<RuleExecutingEventArgs>? OnRuleExecuting;

        /// <summary>
        /// Fired after a rule completes execution.
        /// </summary>
        public event EventHandler<RuleExecutedEventArgs>? OnRuleExecuted;

        /// <summary>
        /// Logs rule execution via Microsoft.Extensions.Logging if a logger is set.
        /// </summary>
        private void LogExecuted(RuleExecutedEvent @event)
        {
            Logger?.LogRuleExecuted(@event);
        }
    }
}
