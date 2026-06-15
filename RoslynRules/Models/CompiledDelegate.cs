using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynRules.Models
{
    /// <summary>
    /// Wraps a typed compiled delegate and provides fast invocation from RuleParameter values.
    /// Eliminates the need for slow DynamicInvoke by extracting parameter values and calling directly.
    /// Supports exactly one input parameter.
    /// </summary>
    internal abstract class CompiledDelegate
    {
        /// <summary>
        /// Invokes the compiled delegate with the value extracted from the RuleParameter.
        /// </summary>
        /// <param name="parameter">Runtime parameter value.</param>
        /// <returns>Delegate return value.</returns>
        public abstract object? Invoke(object? parameter);
    }

    /// <summary>
    /// Typed compiled delegate for functions with 1 parameter returning a value.
    /// </summary>
    /// <typename="TParam">Parameter type.</typename>
    /// <typename="TReturn">Return type.</typename>
    internal sealed class CompiledFunc<TParam, TReturn> : CompiledDelegate
    {
        private readonly Func<TParam, TReturn> _delegate;

        public CompiledFunc(Func<TParam, TReturn> del) => _delegate = del;

        public override object? Invoke(object? parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter), "Rule parameter cannot be null for this delegate.");

            var arg = (TParam)parameter;
            return _delegate(arg);
        }
    }

    /// <summary>
    /// Typed compiled delegate for actions with 1 parameter (void return).
    /// </summary>
    /// <typename="TParam">Parameter type.</typename>
    internal sealed class CompiledAction<TParam> : CompiledDelegate
    {
        private readonly Action<TParam> _delegate;

        public CompiledAction(Action<TParam> del) => _delegate = del;

        public override object? Invoke(object? parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter), "Rule parameter cannot be null for this delegate.");

            var arg = (TParam)parameter;
            _delegate(arg);
            return null;
        }
    }

    /// <summary>
    /// Typed compiled delegate for async functions with 1 parameter returning Task<TReturn>.
    /// The Invoke method unwraps the task and returns the result synchronously.
    /// </summary>
    /// <typename="TParam">Parameter type.</typename>
    /// <typename="TReturn">Return type (inside Task).</typename>
    internal sealed class CompiledAsyncFunc<TParam, TReturn> : CompiledDelegate
    {
        private readonly Func<TParam, Task<TReturn>> _delegate;

        public CompiledAsyncFunc(Func<TParam, Task<TReturn>> del) => _delegate = del;

        public override object? Invoke(object? parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter), "Rule parameter cannot be null for this delegate.");

            var arg = (TParam)parameter;
            // For sync execution, block and unwrap the task.
            // Use ConfigureAwait(false) to avoid deadlock in UI/ASP.NET contexts.
            // Use ExecuteAsync for true async execution.
            var task = _delegate(arg);
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronous invocation that properly awaits the task.
        /// </summary>
        public async Task<object?> InvokeAsync(object? parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter), "Rule parameter cannot be null for this delegate.");

            var arg = (TParam)parameter;
            var result = await _delegate(arg);
            return result;
        }
    }

    /// <summary>
    /// Typed compiled delegate for async actions with 1 parameter returning Task.
    /// </summary>
    /// <typename="TParam">Parameter type.</typename>
    internal sealed class CompiledAsyncAction<TParam> : CompiledDelegate
    {
        private readonly Func<TParam, Task> _delegate;

        public CompiledAsyncAction(Func<TParam, Task> del) => _delegate = del;

        public override object? Invoke(object? parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter), "Rule parameter cannot be null for this delegate.");

            var arg = (TParam)parameter;
            // Block for sync execution path.
            // Use ConfigureAwait(false) to avoid deadlock in UI/ASP.NET contexts.
            _delegate(arg).ConfigureAwait(false).GetAwaiter().GetResult();
            return null;
        }

        /// <summary>
        /// Asynchronous invocation that properly awaits the task.
        /// </summary>
        public async Task<object?> InvokeAsync(object? parameter)
        {
            if (parameter is null)
                throw new ArgumentNullException(nameof(parameter), "Rule parameter cannot be null for this delegate.");

            var arg = (TParam)parameter;
            await _delegate(arg);
            return null;
        }
    }

    /// <summary>
    /// Multi-parameter compiled delegate that uses DynamicInvoke with an object array.
    /// Slightly slower than the typed single-parameter wrappers, but supports any number of parameters.
    /// </summary>
    internal sealed class CompiledMultiParamDelegate : CompiledDelegate
    {
        private readonly Delegate _delegate;

        public CompiledMultiParamDelegate(Delegate del) => _delegate = del;

        public override object? Invoke(object? parameter)
        {
            // For multi-parameter delegates, parameter is expected to be a RuleParameter[]
            if (parameter is RuleParameter[] paramArray)
            {
                var args = paramArray.Select(p => p.Value).ToArray();
                return _delegate.DynamicInvoke(args);
            }
            
            // Single parameter passed directly (fallback)
            return _delegate.DynamicInvoke(parameter);
        }
    }

    /// <summary>
    /// Async multi-parameter compiled delegate that uses DynamicInvoke with an object array.
    /// </summary>
    internal sealed class CompiledAsyncMultiParamDelegate : CompiledDelegate
    {
        private readonly Delegate _delegate;

        public CompiledAsyncMultiParamDelegate(Delegate del) => _delegate = del;

        public override object? Invoke(object? parameter)
        {
            if (parameter is RuleParameter[] paramArray)
            {
                var args = paramArray.Select(p => p.Value).ToArray();
                dynamic task = _delegate.DynamicInvoke(args)!;
                return task.ConfigureAwait(false).GetAwaiter().GetResult();
            }
            
            dynamic singleTask = _delegate.DynamicInvoke(parameter)!;
            return singleTask.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<object?> InvokeAsync(object? parameter)
        {
            if (parameter is RuleParameter[] paramArray)
            {
                var args = paramArray.Select(p => p.Value).ToArray();
                dynamic task = _delegate.DynamicInvoke(args)!;
                return await task;
            }
            
            dynamic singleTask = _delegate.DynamicInvoke(parameter)!;
            return await singleTask;
        }
    }

    /// <summary>
    /// Factory that creates the appropriate CompiledDelegate wrapper from a raw Delegate.
    /// Supports single and multiple input parameters.
    /// Automatically detects async delegates (returning Task or Task<T>) and wraps accordingly.
    /// </summary>
    internal static class CompiledDelegateFactory
    {
        /// <summary>
        /// Wraps a raw Delegate in a typed CompiledDelegate for fast invocation.
        /// Supports single and multi-parameter delegates.
        /// For multi-parameter delegates, uses DynamicInvoke (slower but necessary).
        /// Detects async signatures (Task/Task<T> return types) automatically.
        /// </summary>
        /// <param name="del">The raw compiled delegate.</param>
        /// <returns>A CompiledDelegate wrapper.</returns>
        [RequiresUnreferencedCode("RoslynRules uses reflection to inspect delegate signatures and instantiate generic wrappers. This code may not work correctly with trimming or AOT.")]
        public static CompiledDelegate Wrap(Delegate del)
        {
            var type = del.GetType();
            var invoke = type.GetMethod("Invoke")!;
            var parameters = invoke.GetParameters();
            var returnType = invoke.ReturnType;

            // Multi-parameter delegates use the slower DynamicInvoke path
            if (parameters.Length > 1)
            {
                if (returnType == typeof(Task) || 
                    (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)))
                {
                    return new CompiledAsyncMultiParamDelegate(del);
                }
                return new CompiledMultiParamDelegate(del);
            }

            var paramType = parameters[0].ParameterType;

            // Check for async signatures (Task or Task<T>)
            if (returnType == typeof(Task))
            {
                var wrapperType = typeof(CompiledAsyncAction<>).MakeGenericType(paramType);
                return (CompiledDelegate)Activator.CreateInstance(wrapperType, del)!;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResultType = returnType.GetGenericArguments()[0];
                var wrapperType = typeof(CompiledAsyncFunc<,>).MakeGenericType(paramType, taskResultType);
                return (CompiledDelegate)Activator.CreateInstance(wrapperType, del)!;
            }

            // Synchronous delegates
            if (returnType == typeof(void))
            {
                var wrapperType = typeof(CompiledAction<>).MakeGenericType(paramType);
                return (CompiledDelegate)Activator.CreateInstance(wrapperType, del)!;
            }
            else
            {
                var wrapperType = typeof(CompiledFunc<,>).MakeGenericType(paramType, returnType);
                return (CompiledDelegate)Activator.CreateInstance(wrapperType, del)!;
            }
        }
    }
}
