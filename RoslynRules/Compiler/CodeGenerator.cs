using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// Generates the C# source code string that wraps a user expression.
    /// Produces a static class with a single Evaluate method.
    /// Automatically detects async expressions (containing 'await') and generates
    /// async method signatures when needed.
    /// </summary>
    internal static class CodeGenerator
    {
        // Hardcoded default namespaces included in every generated class.
        private static readonly string[] DefaultUsings = new[]
        {
            "System",
            "System.Linq",
            "System.Collections.Generic",
            "System.Text"
        };

        /// <summary>
        /// Generates a complete C# source file containing a static Evaluate method.
        /// Automatically detects async expressions (containing 'await') and generates
        /// async method signatures when needed.
        /// </summary>
        public static string Generate(
            string expression,
            Type returnType,
            string[] parameterNames,
            Type[] parameterTypes,
            string[]? additionalNamespaces = null)
        {
            var isAsync = ContainsAwaitExpression(expression);
            
            // Unwrap Task/Task<T> to get the logical return type for the method signature
            var logicalReturnType = returnType;
            if (returnType == typeof(Task))
            {
                logicalReturnType = typeof(void);
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                logicalReturnType = returnType.GetGenericArguments()[0];
            }
            
            var methodSignature = BuildMethodSignature(logicalReturnType, parameterNames, parameterTypes, isAsync);
            
            // For async methods: if expression starts with 'await', don't double-await
            // For void return types (sync or async), don't add 'return'
            string returnKeyword;
            if (logicalReturnType == typeof(void))
            {
                returnKeyword = "";
            }
            else if (isAsync)
            {
                returnKeyword = expression.TrimStart().StartsWith("await ") ? "return " : "return await ";
            }
            else
            {
                returnKeyword = "return ";
            }
            var usingStatements = BuildUsingStatements(additionalNamespaces, isAsync);

            var code = $@"
{usingStatements}

public static class ExpressionAssembly
{{
    {methodSignature}
    {{
        {returnKeyword}{expression};
    }}
}}
";
            return code;
        }

        /// <summary>
        /// Builds the method signature line, e.g.:
        /// "public static bool Evaluate(Customer customer)"
        /// or "public static async Task<bool> Evaluate(Customer customer)" for async.
        /// </summary>
        private static string BuildMethodSignature(Type returnType, string[] parameterNames, Type[] parameterTypes, bool isAsync = false)
        {
            var paramDecls = parameterNames.Select((name, i) =>
            {
                var paramType = parameterTypes[i];
                return $"{TypeNameResolver.GetTypeName(paramType)} {name}";
            });

            string signaturePrefix;
            if (isAsync)
            {
                // For async: unwrap Task<T> to T for the method signature
                // e.g., if returnType is Task<bool>, method returns Task<bool> (not Task<Task<bool>>)
                if (returnType == typeof(void))
                    signaturePrefix = "public static async Task Evaluate";
                else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var innerType = returnType.GetGenericArguments()[0];
                    signaturePrefix = $"public static async Task<{TypeNameResolver.GetTypeName(innerType)}> Evaluate";
                }
                else
                    signaturePrefix = $"public static async Task<{TypeNameResolver.GetTypeName(returnType)}> Evaluate";
            }
            else
            {
                signaturePrefix = returnType == typeof(void)
                    ? "public static void Evaluate"
                    : $"public static {TypeNameResolver.GetTypeName(returnType)} Evaluate";
            }

            return $"{signaturePrefix}({string.Join(", ", paramDecls)})";
        }

        /// <summary>
        /// Combines default and additional namespaces into using directives.
        /// Adds System.Threading.Tasks for async expressions.
        /// </summary>
        private static string BuildUsingStatements(string[]? additionalNamespaces, bool isAsync = false)
        {
            var allUsings = new HashSet<string>(DefaultUsings);

            if (isAsync)
                allUsings.Add("System.Threading.Tasks");

            if (additionalNamespaces != null)
            {
                foreach (var ns in additionalNamespaces)
                    allUsings.Add(ns);
            }

            return string.Join("\n", allUsings.Select(u => $"using {u};"));
        }

        /// <summary>
        /// Checks if an expression contains an actual await expression by parsing the syntax tree.
        /// Avoids false positives from variable names, string literals, or comments.
        /// </summary>
        private static bool ContainsAwaitExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return false;

            try
            {
                var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(expression);
                var root = tree.GetRoot();
                return root.DescendantNodes().Any(n => n is Microsoft.CodeAnalysis.CSharp.Syntax.AwaitExpressionSyntax);
            }
            catch
            {
                // If parsing fails, fall back to the simple but fragile check
                return expression.Contains("await");
            }
        }
    }
}
