using System;
using System.Threading;

namespace RoslynRules.Models
{
    /// <summary>
    /// Lightweight, thread-safe execution metrics for a single rule.
    /// Tracks evaluation count, timing, and failure rate with lock-free interlocked operations.
    /// Zero overhead if never accessed — metrics are only updated when explicitly enabled.
    /// </summary>
    public struct RuleMetrics
    {
        private long _evalCount;
        private long _failureCount;
        private long _totalTicks;
        private long _lastExecutedTicks;

        /// <summary>
        /// Creates a metrics snapshot with the specified values.
        /// </summary>
        public RuleMetrics(long evalCount, long failureCount, long totalTicks, long lastExecutedTicks)
        {
            _evalCount = evalCount;
            _failureCount = failureCount;
            _totalTicks = totalTicks;
            _lastExecutedTicks = lastExecutedTicks;
        }

        /// <summary>
        /// Total number of times this rule has been evaluated.
        /// </summary>
        public long EvalCount => _evalCount;

        /// <summary>
        /// Number of times this rule evaluation failed (threw or returned Success=false).
        /// </summary>
        public long FailureCount => _failureCount;

        /// <summary>
        /// Average execution time in milliseconds across all evaluations.
        /// Returns 0 if no evaluations have occurred.
        /// </summary>
        public double AverageExecutionTimeMs => _evalCount > 0
            ? TimeSpan.FromTicks(_totalTicks).TotalMilliseconds / _evalCount
            : 0;

        /// <summary>
        /// Failure rate as a percentage (0.0 to 100.0).
        /// Returns 0 if no evaluations have occurred.
        /// </summary>
        public double FailureRatePercent => _evalCount > 0
            ? (_failureCount / (double)_evalCount) * 100
            : 0;

        /// <summary>
        /// UTC timestamp of the last evaluation. Null if never executed.
        /// </summary>
        public DateTime? LastExecuted => _lastExecutedTicks > 0
            ? new DateTime(_lastExecutedTicks, DateTimeKind.Utc)
            : null;

        /// <summary>
        /// Internal: total elapsed ticks for snapshot construction.
        /// </summary>
        public long TotalTicks => _totalTicks;

        /// <summary>
        /// Records a single evaluation result. Thread-safe.
        /// </summary>
        /// <param name="elapsedTicks">Execution duration in Stopwatch ticks.</param>
        /// <param name="failed">True if the evaluation failed.</param>
        public void Record(long elapsedTicks, bool failed)
        {
            Interlocked.Increment(ref _evalCount);
            Interlocked.Add(ref _totalTicks, elapsedTicks);

            if (failed)
                Interlocked.Increment(ref _failureCount);

            Interlocked.Exchange(ref _lastExecutedTicks, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Resets all metrics to zero. Thread-safe.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _evalCount, 0);
            Interlocked.Exchange(ref _failureCount, 0);
            Interlocked.Exchange(ref _totalTicks, 0);
            Interlocked.Exchange(ref _lastExecutedTicks, 0);
        }
    }
}
