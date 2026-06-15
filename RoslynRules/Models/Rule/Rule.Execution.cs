using RoslynRules.Exceptions;
using RoslynRules.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoslynRules.Models
{
    public sealed partial class Rule
    {
        // ==================== EXECUTION ====================

        /// <summary>
        /// Executes the rule bottom-up: children first, then Expression, then Action.
        /// Returns a RuleResult indicating success or failure.
        /// For async expressions, use ExecuteAsync.
        /// Fires logging events if Logger is set.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Result of the rule evaluation.</returns>
        public RuleResult Execute(params RuleParameter[] parameters)
            => ExecuteWithContext(null, parameters);

        /// <summary>
        /// Executes the rule with access to dependency results via a RuleContext.
        /// The context provides access to the outputs of rules this rule depends on.
        /// </summary>
        /// <param name="context">Context containing results of previously executed rules.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Result of the rule evaluation.</returns>
        public RuleResult ExecuteWithContext(RuleContext? context, params RuleParameter[] parameters)
        {
            var sw = Stopwatch.StartNew();
            RuleResult result;
            Exception? exception = null;

            try
            {
                result = ExecuteCore(context, parameters);
            }
            catch (RulesException)
            {
                throw; // Re-throw setup/compilation errors
            }
            catch (Exception ex)
            {
                exception = ex;
                result = new RuleResult(false, Id, GetLocalizedDescription(), IsActive, Exception: ex);
            }

            sw.Stop();

            // Record metrics
            var failed = exception != null || !result.Success;
            _metrics.Record(sw.ElapsedTicks, failed);

            LogExecuted(new RuleExecutedEvent
            {
                RuleId = Id,
                RuleDescription = GetLocalizedDescription(),
                IsActive = IsActive,
                Success = result.Success,
                ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
                Exception = exception
            });

            // Store result in context for dependent rules
            context?.StoreResult(Id, result);

            return result;
        }

        /// <summary>
        /// Core execution logic without logging overhead.
        /// Enforces per-rule timeout if configured.
        /// </summary>
        /// <param name="context">Optional context for accessing dependency results.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        private RuleResult ExecuteCore(RuleContext? context, RuleParameter[] parameters)
        {
            if (!IsActive)
                return new RuleResult(true, Id, GetLocalizedDescription(), IsActive);

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // If timeout is configured, wrap execution in a timeout.
            // NOTE: This blocks a thread-pool thread. For production use with timeouts,
            // prefer ExecuteAsync() which uses cooperative cancellation without blocking.
            if (Timeout.HasValue)
            {
                using var cts = new CancellationTokenSource();
                var workTask = Task.Run(() => ExecuteCoreInternal(context, parameters, cts.Token), cts.Token);
                var timeoutTask = Task.Delay(Timeout.Value, cts.Token);
                var completed = Task.WhenAny(workTask, timeoutTask).GetAwaiter().GetResult();

                if (completed == timeoutTask)
                {
                    cts.Cancel(); // Signal cancellation to the work task
                    throw new RuleTimeoutException(Id, Timeout.Value);
                }

                return workTask.GetAwaiter().GetResult();
            }

            return ExecuteCoreInternal(context, parameters);
        }

        /// <summary>
        /// Validates that execution-time parameters match the compile-time schema.
        /// Checks parameter count, name match, and type compatibility.
        /// </summary>
        /// <param name="parameters">Runtime parameters passed to Execute.</param>
        /// <exception cref="RuleValidationException">Thrown when parameter name or type mismatch is detected.</exception>
        private void ValidateExecutionParameters(RuleParameter[] parameters)
        {
            // Skip validation if this rule has no compiled delegates (e.g., only child rules).
            if (_compiledParameters.Length == 0)
                return;

            if (parameters.Length != _compiledParameters.Length)
            {
                throw new RuleValidationException(
                    $"Parameter count mismatch for rule '{Description}' (Id: {Id}). " +
                    $"Expected {_compiledParameters.Length} parameters (compiled), but received {parameters.Length}. " +
                    "Ensure Execute() uses the same parameter count as Compile().");
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                var compiled = _compiledParameters[i];
                var runtime = parameters[i];

                // Validate name matches compile-time name.
                if (!string.Equals(runtime.Name, compiled.Name, StringComparison.Ordinal))
                {
                    throw new RuleValidationException(
                        $"Parameter name mismatch for rule '{Description}' (Id: {Id}). " +
                        $"Expected parameter name '{compiled.Name}' (compiled), but received '{runtime.Name}'. " +
                        "Ensure Execute() uses the same parameter names as Compile().");
                }

                // Validate type is assignable to compile-time type.
                if (!runtime.Type.IsAssignableTo(compiled.Type))
                {
                    var valueTypeName = runtime.Value?.GetType()?.Name ?? "null";
                    throw new RuleValidationException(
                        $"Parameter type mismatch for rule '{Description}' (Id: {Id}), parameter '{runtime.Name}'. " +
                        $"Expected type '{compiled.Type.Name}' (compiled), but received '{runtime.Type.Name}'. " +
                        $"Value type '{valueTypeName}' is not assignable to '{compiled.Type.Name}'.");
                }
            }
        }

        /// <summary>
        /// Core execution logic without timeout or logging.
        /// Fires OnRuleExecuting and OnRuleExecuted lifecycle events.
        /// Exceptions propagate naturally — caught by ExecuteWithContext for logging.
        /// </summary>
        private RuleResult ExecuteCoreInternal(RuleContext? context, RuleParameter[] parameters, CancellationToken cancellationToken = default)
        {
            if (!IsActive)
                return new RuleResult(true, Id, GetLocalizedDescription(), IsActive);

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // Validate execute-time parameters match compile-time schema.
            ValidateExecutionParameters(parameters);

            // Check cache first
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                if (_resultCache.TryGet(cacheKey, out var cachedResult))
                    return cachedResult;
            }

            var sw = Stopwatch.StartNew();
            RuleResult result;

            // Fire OnRuleExecuting event
            var executingArgs = new RuleExecutingEventArgs(this, parameters);
            OnRuleExecuting?.Invoke(this, executingArgs);
            if (executingArgs.Cancel)
            {
                result = new RuleResult(true, Id, GetLocalizedDescription(), IsActive, Value: null,
                    ChildResults: new List<RuleResult>(),
                    Exception: executingArgs.CancelReason != null
                        ? new OperationCanceledException(executingArgs.CancelReason)
                        : null);
                goto Completed;
            }

            // For multi-parameter delegates, pass the full RuleParameter[] array.
            // For single-parameter delegates, pass just the value directly (fast path).
            var paramValue = _compiledParameters.Length > 1 ? parameters : parameters[0].Value;

            // Bottom-up: evaluate all active children first
            var childResults = new List<RuleResult>();
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childResult = child.ExecuteWithContext(context, parameters);
                childResults.Add(childResult);
                if (!childResult.Success)
                {
                    result = new RuleResult(false, Id, GetLocalizedDescription(), IsActive, ChildResults: childResults);
                    goto Completed;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Evaluate compiled Expression if present
            if (_compiledExpression != null)
            {
                var exprResult = _compiledExpression.Invoke(paramValue);
                if (!(bool)exprResult!)
                {
                    result = new RuleResult(false, Id, GetLocalizedDescription(), IsActive, ChildResults: childResults);
                    goto Completed;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Execute compiled Action if present
            if (_compiledAction != null)
            {
                var actionResult = _compiledAction.Invoke(paramValue);
                result = new RuleResult(true, Id, GetLocalizedDescription(), IsActive, actionResult, ChildResults: childResults);
                goto Completed;
            }

            result = new RuleResult(true, Id, GetLocalizedDescription(), IsActive, ChildResults: childResults);

        Completed:
            sw.Stop();

            // Fire OnRuleExecuted event
            var executedArgs = new RuleExecutedEventArgs(this, result, sw.Elapsed);
            OnRuleExecuted?.Invoke(this, executedArgs);

            // Store result in cache
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                _resultCache.Set(cacheKey, result, CacheDuration.Value);
            }

            return result;
        }

        // ==================== ASYNC EXECUTION ====================

        /// <summary>
        /// Executes the rule asynchronously. Supports async expressions containing await.
        /// Children are executed sequentially (bottom-up dependency), but their internal
        /// async operations are properly awaited.
        /// Fires logging events if Logger is set.
        /// </summary>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Task containing the rule evaluation result.</returns>
        public Task<RuleResult> ExecuteAsync(params RuleParameter[] parameters)
            => ExecuteWithContextAsync(null, parameters);

        /// <summary>
        /// Executes the rule asynchronously with access to dependency results via a RuleContext.
        /// The context provides access to the outputs of rules this rule depends on.
        /// </summary>
        /// <param name="context">Context containing results of previously executed rules.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        /// <returns>Task containing the rule evaluation result.</returns>
        public async Task<RuleResult> ExecuteWithContextAsync(RuleContext? context, params RuleParameter[] parameters)
        {
            var sw = Stopwatch.StartNew();
            RuleResult result;
            Exception? exception = null;

            try
            {
                result = await ExecuteCoreAsync(context, parameters);
            }
            catch (RulesException)
            {
                throw; // Re-throw setup/compilation errors
            }
            catch (Exception ex)
            {
                exception = ex;
                result = new RuleResult(false, Id, GetLocalizedDescription(), IsActive, Exception: ex);
            }

            sw.Stop();

            // Record metrics
            var failed = exception != null || !result.Success;
            _metrics.Record(sw.ElapsedTicks, failed);

            LogExecuted(new RuleExecutedEvent
            {
                RuleId = Id,
                RuleDescription = GetLocalizedDescription(),
                IsActive = IsActive,
                Success = result.Success,
                ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds,
                Exception = exception
            });

            // Store result in context for dependent rules
            context?.StoreResult(Id, result);

            return result;
        }

        /// <summary>
        /// Core async execution logic without logging overhead.
        /// Enforces per-rule timeout if configured.
        /// Fires OnRuleExecuting and OnRuleExecuted lifecycle events.
        /// Exceptions propagate naturally — caught by ExecuteWithContextAsync for logging.
        /// </summary>
        /// <param name="context">Optional context for accessing dependency results.</param>
        /// <param name="parameters">Runtime parameter values.</param>
        private async Task<RuleResult> ExecuteCoreAsync(RuleContext? context, RuleParameter[] parameters)
        {
            if (!IsActive)
                return new RuleResult(true, Id, GetLocalizedDescription(), IsActive);

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // Check cache first
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                if (_resultCache.TryGet(cacheKey, out var cachedResult))
                    return cachedResult;
            }

            var sw = Stopwatch.StartNew();
            RuleResult result;

            // Fire OnRuleExecuting event
            var executingArgs = new RuleExecutingEventArgs(this, parameters);
            OnRuleExecuting?.Invoke(this, executingArgs);
            if (executingArgs.Cancel)
            {
                result = new RuleResult(true, Id, GetLocalizedDescription(), IsActive, Value: null,
                    ChildResults: new List<RuleResult>(),
                    Exception: executingArgs.CancelReason != null
                        ? new OperationCanceledException(executingArgs.CancelReason)
                        : null);
            }
            else if (Timeout.HasValue)
            {
                // If timeout is configured, wrap execution in a timed task
                using var cts = new CancellationTokenSource((int)Timeout.Value.TotalMilliseconds);
                var task = ExecuteCoreAsyncInternal(context, parameters, cts.Token);
                var completed = await Task.WhenAny(task, Task.Delay(Timeout.Value, cts.Token));
                if (completed != task)
                    throw new RuleTimeoutException(Id, Timeout.Value);
                result = await task;
            }
            else
            {
                result = await ExecuteCoreAsyncInternal(context, parameters, CancellationToken.None);
            }

            sw.Stop();

            // Fire OnRuleExecuted event
            var executedArgs = new RuleExecutedEventArgs(this, result, sw.Elapsed);
            OnRuleExecuted?.Invoke(this, executedArgs);

            // Store result in cache
            if (CacheDuration.HasValue)
            {
                var cacheKey = Execution.CacheKeyBuilder.Build(Id, parameters);
                _resultCache.Set(cacheKey, result, CacheDuration.Value);
            }

            return result;
        }

        /// <summary>
        /// Core async execution logic without timeout or logging.
        /// </summary>
        private async Task<RuleResult> ExecuteCoreAsyncInternal(RuleContext? context, RuleParameter[] parameters, CancellationToken cancellationToken)
        {
            if (!IsActive)
                return new RuleResult(true, Id, GetLocalizedDescription(), IsActive);

            if (_compiledExpression == null && _compiledAction == null && !ChildRules.Any())
                throw new NotCompiledException(Id);

            // Validate execute-time parameters match compile-time schema.
            ValidateExecutionParameters(parameters);

            // For multi-parameter delegates, pass the full RuleParameter[] array.
            // For single-parameter delegates, pass just the value directly (fast path).
            var paramValue = _compiledParameters.Length > 1 ? parameters : parameters[0].Value;

            // Bottom-up: evaluate all active children first (async)
            var childResults = new List<RuleResult>();
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childResult = await child.ExecuteWithContextAsync(context, parameters);
                childResults.Add(childResult);
                if (!childResult.Success)
                return new RuleResult(false, Id, GetLocalizedDescription(), IsActive, ChildResults: childResults);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Evaluate compiled Expression if present
            if (_compiledExpression != null)
            {
                object? exprResult;
                if (_compiledExpression is CompiledAsyncFunc<object?, object?> asyncExpr)
                {
                    exprResult = await asyncExpr.InvokeAsync(paramValue);
                }
                else
                {
                    exprResult = _compiledExpression.Invoke(paramValue);
                }
                
                if (!(bool)exprResult!)
                return new RuleResult(false, Id, GetLocalizedDescription(), IsActive, ChildResults: childResults);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Execute compiled Action if present
            if (_compiledAction != null)
            {
                object? actionResult;
                if (_compiledAction is CompiledAsyncAction<object?> asyncAction)
                {
                    actionResult = await asyncAction.InvokeAsync(paramValue);
                }
                else
                {
                    actionResult = _compiledAction.Invoke(paramValue);
                }
                return new RuleResult(true, Id, GetLocalizedDescription(), IsActive, actionResult, ChildResults: childResults);
            }

                return new RuleResult(true, Id, GetLocalizedDescription(), IsActive, ChildResults: childResults);
        }
    }
}
