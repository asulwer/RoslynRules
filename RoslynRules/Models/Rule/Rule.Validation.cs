using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynRules.Compiler;
using RoslynRules.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynRules.Models
{
    public sealed partial class Rule
    {
        // ==================== VALIDATION ====================

        /// <summary>
        /// Validates the rule structure and expression syntax before compilation.
        /// Checks that the rule has valid content, that expressions are syntactically
        /// correct C#, and that child rules are valid.
        /// <br/><br/>
        /// <b>Note:</b> This method only validates <i>syntax</i>, not semantics.
        /// Undefined variables or missing types will pass syntax validation but fail
        /// at compile time. Use <see cref="ValidateSemantics">ValidateSemantics</see>
        /// for full semantic validation (requires a compiler and parameters).
        /// </summary>
        /// <exception cref="RuleValidationException">Thrown when structural or syntax validation fails.</exception>
        public void Validate(IEnumerable<Guid>? availableRuleIds = null)
        {
            // 1. Structural validation: a rule must have something to do.
            if (string.IsNullOrEmpty(Expression) && string.IsNullOrEmpty(Action) && !ChildRules.Any(r => r.IsActive))
            {
                throw new RuleValidationException(
                    $"Rule &apos;{Description}&apos; (Id: {Id}) has no Expression, Action, or active ChildRules.");
            }

            // 2. Validate no circular references FIRST (before recursing into children).
            ValidateNoCircularReferences();

            // 3. Validate Expression syntax if present.
            if (!string.IsNullOrEmpty(Expression))
            {
                ValidateExpressionSyntax(Expression, mustReturnBool: true);
            }

            // 4. Validate Action syntax if present.
            if (!string.IsNullOrEmpty(Action))
            {
                ValidateExpressionSyntax(Action, mustReturnBool: false);
            }

            // 5. Validate dependency references if available rule IDs are provided.
            if (DependsOnRuleId.HasValue && availableRuleIds != null)
            {
                var available = availableRuleIds as ICollection<Guid> ?? availableRuleIds.ToList();
                if (!available.Contains(DependsOnRuleId.Value))
                {
                    throw new RuleValidationException(
                        $"Rule '{Description}' (Id: {Id}) depends on rule {DependsOnRuleId.Value} which does not exist or is inactive in the current workflow.");
                }
            }

            // 6. Validate child rules recursively (safe now that circular refs are checked).
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.Validate(availableRuleIds);
            }
        }

        /// <summary>
        /// Validates the rule and all child rules, returning all errors found.
        /// Does not throw — returns an empty array if validation succeeds.
        /// </summary>        /// <returns>Array of validation errors. Empty if valid.</returns>
        public ValidationError[] ValidateAll(IEnumerable<Guid>? availableRuleIds = null)
        {
            var errors = new List<ValidationError>();

            // 1. Structural validation: a rule must have something to do.
            if (string.IsNullOrEmpty(Expression) && string.IsNullOrEmpty(Action) && !ChildRules.Any(r => r.IsActive))
            {
                errors.Add(new ValidationError(
                    $"Rule &apos;{Description}&apos; (Id: {Id}) has no Expression, Action, or active ChildRules.",
                    ValidationErrorType.EmptyRule, Id, Description));
                return errors.ToArray();
            }

            // 2. Validate no circular references FIRST.
            try
            {
                ValidateNoCircularReferences();
            }
            catch (CircularReferenceException ex)
            {
                errors.Add(new ValidationError(
                    ex.Message, ValidationErrorType.CircularReference, ex.RuleId, ex.RuleDescription));
            }

            // 3. Validate Expression syntax if present.
            if (!string.IsNullOrEmpty(Expression))
            {
                ValidateExpressionSyntax(Expression, mustReturnBool: true, errors);
            }

            // 4. Validate Action syntax if present.
            if (!string.IsNullOrEmpty(Action))
            {
                ValidateExpressionSyntax(Action, mustReturnBool: false, errors);
            }

            // 5. Validate dependency references if available rule IDs are provided.
            if (DependsOnRuleId.HasValue && availableRuleIds != null)
            {
                var available = availableRuleIds as ICollection<Guid> ?? availableRuleIds.ToList();
                if (!available.Contains(DependsOnRuleId.Value))
                {
                    errors.Add(new ValidationError(
                        $"Rule '{Description}' (Id: {Id}) depends on rule {DependsOnRuleId.Value} which does not exist or is inactive in the current workflow.",
                        ValidationErrorType.MissingDependency, Id, Description));
                }
            }

            // 6. Validate child rules recursively.
            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                errors.AddRange(child.ValidateAll(availableRuleIds));
            }

            return errors.ToArray();
        }

        /// <summary>
        /// Validates that a C# expression string is syntactically valid.
        /// Uses Roslyn to parse the expression without compiling it.
        /// Throws on syntax errors.
        /// </summary>
        private static void ValidateExpressionSyntax(string expression, bool mustReturnBool)
        {
            var code = $@"class X {{ void M() {{ {(mustReturnBool ? "var __result = " : "")}{expression}; }} }}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = tree.GetDiagnostics();
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
            {
                throw new SyntaxErrorException(expression, errors.Select(e => e.GetMessage()).ToArray());
            }
        }

        /// <summary>
        /// Validates that a C# expression string is syntactically valid.
        /// Uses Roslyn to parse the expression without compiling it.
        /// Errors are collected into the provided list instead of thrown.
        /// </summary>
        private static void ValidateExpressionSyntax(string expression, bool mustReturnBool, List<ValidationError> errors)
        {
            var code = $@"class X {{ void M() {{ {(mustReturnBool ? "var __result = " : "")}{expression}; }} }}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = tree.GetDiagnostics();
            var syntaxErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (syntaxErrors.Any())
            {
                errors.Add(new ValidationError(
                    $"Syntax error in expression: {string.Join("; ", syntaxErrors.Select(e => e.GetMessage()))}",
                    ValidationErrorType.SyntaxError));
            }
        }

        // ==================== SEMANTIC VALIDATION ====================

        /// <summary>
        /// Performs semantic validation by attempting to compile the rule's Expression and Action.
        /// This catches errors that syntax-only validation misses: undefined identifiers,
        /// missing types, incorrect method signatures, missing using directives, etc.
        /// </summary>
        /// <param name="compiler">Expression compiler for the dry-run compilation.</param>
        /// <param name="parameters">Parameter definitions used for compilation.</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        /// <exception cref="RuleCompilationException">Thrown if semantic errors are found in the Expression or Action.</exception>
        public void ValidateSemantics(ExpressionCompiler compiler, RuleParameter[] parameters, string[]? additionalNamespaces = null)
        {
            if (!string.IsNullOrEmpty(Expression))
            {
                try
                {
                    var delegateType = BuildDelegateType(typeof(object), parameters);
                    var paramNames = parameters.Select(p => p.Name).ToArray();
                    var exprDelegate = CompileDelegate(compiler, Expression, delegateType, paramNames, additionalNamespaces);
                }
                catch (Exception ex)
                {
                    throw new RuleCompilationException(
                        $"Semantic error in rule '{Description}' (Id: {Id}) Expression: {ex.Message}", ex);
                }
            }

            if (!string.IsNullOrEmpty(Action))
            {
                try
                {
                    var delegateType = BuildDelegateType(typeof(void), parameters);
                    var paramNames = parameters.Select(p => p.Name).ToArray();
                    var actionDelegate = CompileDelegate(compiler, Action, delegateType, paramNames, additionalNamespaces);
                }
                catch (Exception ex)
                {
                    throw new RuleCompilationException(
                        $"Semantic error in rule '{Description}' (Id: {Id}) Action: {ex.Message}", ex);
                }
            }

            foreach (var child in ChildRules.Where(r => r.IsActive))
            {
                child.ValidateSemantics(compiler, parameters, additionalNamespaces);
            }
        }

        // ==================== STATIC VALIDATE SEMANTICS OVERLOADS ====================

        /// <summary>
        /// Validates the semantic correctness of an expression string without creating a Rule instance.
        /// Creates a default ExpressionCompiler internally and validates that the expression
        /// compiles successfully with the given parameter type.
        /// </summary>
        /// <param name="expression">The C# expression to validate.</param>
        /// <param name="parameterType">The type of the parameter used in the expression.</param>
        /// <param name="parameterName">The name of the parameter used in the expression. Defaults to "param".</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        /// <exception cref="RuleCompilationException">Thrown if the expression has semantic errors.</exception>
        public static void ValidateSemantics(string expression, Type parameterType, string parameterName = "param", string[]? additionalNamespaces = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression cannot be null or whitespace.", nameof(expression));

            var rule = new Rule { Expression = expression, Description = "Semantic validation" };
            var compiler = new ExpressionCompiler();
            var parameters = new[] { new RuleParameter(parameterName, parameterType) };

            rule.ValidateSemantics(compiler, parameters, additionalNamespaces);
        }

        /// <summary>
        /// Validates the semantic correctness of an expression string without creating a Rule instance.
        /// Convenience overload that accepts the parameter type as a string (full type name or alias).
        /// </summary>
        /// <param name="expression">The C# expression to validate.</param>
        /// <param name="parameterTypeName">The full type name or alias (e.g., "System.String", "int", "bool").</param>
        /// <param name="parameterName">The name of the parameter used in the expression. Defaults to "param".</param>
        /// <param name="additionalNamespaces">Extra namespaces for expression compilation.</param>
        /// <exception cref="RuleCompilationException">Thrown if the expression has semantic errors.</exception>
        /// <exception cref="ArgumentException">Thrown if the type name cannot be resolved.</exception>
        public static void ValidateSemantics(string expression, string parameterTypeName, string parameterName = "param", string[]? additionalNamespaces = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Expression cannot be null or whitespace.", nameof(expression));

            var parameterType = ResolveTypeFromName(parameterTypeName);
            ValidateSemantics(expression, parameterType, parameterName, additionalNamespaces);
        }

        /// <summary>
        /// Resolves a type from its full name or common alias (e.g., "int", "string", "bool").
        /// </summary>
        /// <param name="typeName">The type name or alias.</param>
        /// <returns>The resolved Type.</returns>
        /// <exception cref="ArgumentException">Thrown if the type cannot be resolved.</exception>
        private static Type ResolveTypeFromName(string typeName)
        {
            // Try common aliases first
            var aliasMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["bool"] = typeof(bool),
                ["byte"] = typeof(byte),
                ["sbyte"] = typeof(sbyte),
                ["char"] = typeof(char),
                ["decimal"] = typeof(decimal),
                ["double"] = typeof(double),
                ["float"] = typeof(float),
                ["int"] = typeof(int),
                ["uint"] = typeof(uint),
                ["long"] = typeof(long),
                ["ulong"] = typeof(ulong),
                ["short"] = typeof(short),
                ["ushort"] = typeof(ushort),
                ["string"] = typeof(string),
                ["object"] = typeof(object),
            };

            if (aliasMap.TryGetValue(typeName, out var aliasedType))
                return aliasedType;

            // Try Type.GetType with current assembly and mscorlib
            var resolvedType = Type.GetType(typeName, throwOnError: false)
                ?? Type.GetType($"{typeName}, System.Private.CoreLib", throwOnError: false)
                ?? Type.GetType($"{typeName}, mscorlib", throwOnError: false);

            if (resolvedType != null)
                return resolvedType;

            // Search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                resolvedType = assembly.GetType(typeName, throwOnError: false);
                if (resolvedType != null)
                    return resolvedType;
            }

            throw new ArgumentException($"Could not resolve type '{typeName}'. Use the full type name (e.g., 'System.DateTime') or a common alias (e.g., 'int', 'string').", nameof(typeName));
        }

        // ==================== CIRCULAR REFERENCE VALIDATION ====================

        /// <summary>
        /// Validates that no circular references exist in the child rule tree.
        /// A rule cannot reference itself or an ancestor as a child.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when a circular reference is detected.</exception>
        private void ValidateNoCircularReferences()
        {
            var visited = new HashSet<Guid>();
            ValidateNoCircularReferences(this, visited);
        }

        /// <summary>
        /// Recursive helper that walks the child rule tree looking for loops.
        /// </summary>
        /// <param name="current">The current rule being checked.</param>
        /// <param name="visited">Set of rule IDs in the current path.</param>
        private static void ValidateNoCircularReferences(Rule current, HashSet<Guid> visited)
        {
            if (!visited.Add(current.Id))
            {
                throw new CircularReferenceException(current.Id, current.Description);
            }

            foreach (var child in current.ChildRules.Where(r => r.IsActive))
            {
                ValidateNoCircularReferences(child, visited);
            }

            visited.Remove(current.Id); // Allow same rule in different branches of the tree
        }
    }
}
