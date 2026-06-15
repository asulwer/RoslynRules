using Microsoft.CodeAnalysis.CSharp;
using RoslynRules.Exceptions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynRules.Models
{
    public sealed partial class Rule
    {
        // ==================== COMPILATION ====================

        /// <summary>
        /// Compiles the Expression and Action into delegates using the supplied compiler.
        /// Validates against circular child references before compilation.
        /// After compilation, all properties become immutable to ensure thread safety.
        /// Recursively compiles all active child rules.
        /// </summary>
        /// <param name="compiler">The expression compiler instance.</param>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        /// <param name="referenceProvider">Optional custom assembly reference provider for sandboxing.</param>
        public void Compile(Compiler.ExpressionCompiler compiler, RuleParameter[] parameters, string[]? additionalNamespaces = null, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            AotCompatibility.ThrowIfAot(nameof(Rule.Compile));

            // Store parameter schemas for execution-time validation.
            _compiledParameters = parameters;

            // Validate no circular references before compiling.
            ValidateNoCircularReferences();

            if (!string.IsNullOrEmpty(Expression))
            {
                var isAsync = IsAsyncExpression(Expression);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(bool), parameters)
                    : BuildDelegateType(typeof(bool), parameters);
                var paramNames = parameters.Select(p => p.Name).ToArray();
                var rawDelegate = CompileDelegate(compiler, Expression, delegateType, paramNames, additionalNamespaces, referenceProvider);
                _compiledExpression = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            if (!string.IsNullOrEmpty(Action))
            {
                var isAsync = IsAsyncExpression(Action);
                var delegateType = isAsync
                    ? BuildAsyncDelegateType(typeof(void), parameters)
                    : BuildDelegateType(typeof(void), parameters);
                var paramNames = parameters.Select(p => p.Name).ToArray();
                var rawDelegate = CompileDelegate(compiler, Action, delegateType, paramNames, additionalNamespaces, referenceProvider);
                _compiledAction = CompiledDelegateFactory.Wrap(rawDelegate);
            }

            // Compile children recursively
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Compile(compiler, parameters, additionalNamespaces, referenceProvider);
            }

            _isCompiled = true; // Lock properties after compilation
        }

        /// <summary>
        /// Checks if an expression contains async/await keywords by parsing the syntax tree.
        /// This avoids false positives from variable names like "awaiting" or string literals.
        /// </summary>
        /// <param name="expression">The expression string.</param>
        /// <returns>True if the expression contains an await expression node.</returns>
        private static bool IsAsyncExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(expression);
                var root = tree.GetRoot();
                return root.DescendantNodes().Any(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.AwaitExpressionSyntax);
            }
            catch
            {
                // If parsing fails, fall back to the simple but fragile check
                return expression.Contains("await");
            }
        }

        /// <summary>
        /// Builds a Func or Action delegate type matching the parameter signature.
        /// Supports any number of parameters.
        /// </summary>
        /// <example>
        /// Single input, single return: Func&lt;Customer, bool&gt;
        /// Single input, void return: Action&lt;Customer&gt;
        /// Multiple inputs: Func&lt;int, string, bool&gt;
        /// </example>
        [RequiresUnreferencedCode("RoslynRules builds delegate types via MakeGenericType which trimming may strip.")]
        private static Type BuildDelegateType(Type returnType, RuleParameter[] parameters)
        {
            var paramTypes = parameters.Select(p => p.Type).ToArray();
            
            if (returnType == typeof(void))
            {
                if (paramTypes.Length == 0)
                    return typeof(Action);
                
                // Action<T1, T2, ...> — up to 16 params natively supported
                var actionType = GetActionType(paramTypes.Length);
                return actionType.MakeGenericType(paramTypes);
            }
            else
            {
                // Func<T1, T2, ..., TReturn> — up to 16 params natively supported
                var allTypes = paramTypes.Concat(new[] { returnType }).ToArray();
                var funcType = GetFuncType(allTypes.Length);
                return funcType.MakeGenericType(allTypes);
            }
        }

        /// <summary>
        /// Builds an async delegate type returning Task&lt;TReturn&gt; or Task.
        /// Supports any number of parameters.
        /// </summary>
        /// <param name="returnType">The result type (not Task, the inner type).</param>
        /// <param name="parameters">Parameter definitions.</param>
        /// <returns>Func&lt;T1, T2, ..., Task&lt;TReturn&gt;&gt; or Func&lt;T1, T2, ..., Task&gt;.</returns>
        [RequiresUnreferencedCode("RoslynRules builds async delegate types via MakeGenericType which trimming may strip.")]
        private static Type BuildAsyncDelegateType(Type returnType, RuleParameter[] parameters)
        {
            var paramTypes = parameters.Select(p => p.Type).ToArray();
            
            if (returnType == typeof(void))
            {
                var allTypes = paramTypes.Concat(new[] { typeof(Task) }).ToArray();
                var funcType = GetFuncType(allTypes.Length);
                return funcType.MakeGenericType(allTypes);
            }
            else
            {
                var taskType = typeof(Task<>).MakeGenericType(returnType);
                var allTypes = paramTypes.Concat(new[] { taskType }).ToArray();
                var funcType = GetFuncType(allTypes.Length);
                return funcType.MakeGenericType(allTypes);
            }
        }

        /// <summary>
        /// Gets the open generic Func type for the given arity (number of type parameters).
        /// Supports up to 16 parameters (Func`17).
        /// </summary>
        private static Type GetFuncType(int arity)
        {
            return arity switch
            {
                2 => typeof(Func<,>),
                3 => typeof(Func<,,>),
                4 => typeof(Func<,,,>),
                5 => typeof(Func<,,,,>),
                6 => typeof(Func<,,,,,>),
                7 => typeof(Func<,,,,,,>),
                8 => typeof(Func<,,,,,,,>),
                9 => typeof(Func<,,,,,,,,>),
                10 => typeof(Func<,,,,,,,,,>),
                11 => typeof(Func<,,,,,,,,,,>),
                12 => typeof(Func<,,,,,,,,,,,>),
                13 => typeof(Func<,,,,,,,,,,,,>),
                14 => typeof(Func<,,,,,,,,,,,,,>),
                15 => typeof(Func<,,,,,,,,,,,,,,>),
                16 => typeof(Func<,,,,,,,,,,,,,,,>),
                17 => typeof(Func<,,,,,,,,,,,,,,,,>),
                _ => throw new NotSupportedException($"Rules support up to 16 parameters. Found {arity - 1} parameters.")
            };
        }

        /// <summary>
        /// Gets the open generic Action type for the given arity (number of type parameters).
        /// Supports up to 16 parameters (Action`16).
        /// </summary>
        private static Type GetActionType(int arity)
        {
            return arity switch
            {
                1 => typeof(Action<>),
                2 => typeof(Action<,>),
                3 => typeof(Action<,,>),
                4 => typeof(Action<,,,>),
                5 => typeof(Action<,,,,>),
                6 => typeof(Action<,,,,,>),
                7 => typeof(Action<,,,,,,>),
                8 => typeof(Action<,,,,,,,>),
                9 => typeof(Action<,,,,,,,,>),
                10 => typeof(Action<,,,,,,,,,>),
                11 => typeof(Action<,,,,,,,,,,>),
                12 => typeof(Action<,,,,,,,,,,,>),
                13 => typeof(Action<,,,,,,,,,,,,>),
                14 => typeof(Action<,,,,,,,,,,,,,>),
                15 => typeof(Action<,,,,,,,,,,,,,,>),
                16 => typeof(Action<,,,,,,,,,,,,,,,>),
                _ => throw new NotSupportedException($"Rules support up to 16 parameters. Found {arity} parameters.")
            };
        }

        /// <summary>
        /// Invokes the compiler via reflection to create a typed delegate.
        /// </summary>
        [RequiresUnreferencedCode("RoslynRules invokes the compiler via reflection (GetMethod, MakeGenericMethod). This code may not work correctly with trimming or AOT.")]
        private static Delegate CompileDelegate(Compiler.ExpressionCompiler compiler, string expression, Type delegateType, string[] paramNames, string[]? additionalNamespaces, Compiler.AssemblyReferenceProvider? referenceProvider = null)
        {
            var method = compiler.GetType().GetMethod("Compile")!.MakeGenericMethod(delegateType);
            var result = method.Invoke(compiler, new object?[] { expression, paramNames, additionalNamespaces ?? Array.Empty<string>(), referenceProvider });
            if (result is not Delegate delegateResult)
                throw new RuleCompilationException("Compiler did not return a valid delegate.");
            return delegateResult;
        }
    }
}
