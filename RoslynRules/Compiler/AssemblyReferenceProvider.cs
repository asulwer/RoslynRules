using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// Scopes which assemblies are made available as <em>references</em> when compiling
    /// expressions, so accidental use of unrelated assemblies fails to compile.
    /// <para>
    /// IMPORTANT: this is NOT a security sandbox. Expression strings are compiled as
    /// trusted C# and run in-process with full trust. Reference scoping does not stop a
    /// determined expression from reaching dangerous types: core types such as
    /// <c>System.IO.File</c>, <c>System.Diagnostics.Process</c>, and all of reflection
    /// live in always-referenced core assemblies (System.Private.CoreLib / System.Runtime),
    /// and reflection (<c>Type.GetType(...).GetMethod(...).Invoke(...)</c>) can reach anything.
    /// Never compile expression strings that originate from untrusted input. If you must
    /// evaluate untrusted rules, run them in a separate OS-sandboxed process.
    /// </para>
    /// </summary>
    public class AssemblyReferenceProvider
    {
        private readonly HashSet<string> _allowedAssemblyNames;
        private readonly HashSet<string> _blockedAssemblyNames;

        /// <summary>
        /// The default safe whitelist. Core .NET types, LINQ, JSON, and Roslyn compiler internals.
        /// </summary>
        public static readonly string[] DefaultWhitelist = new[]
        {
            "System.Runtime",
            "System.Private.CoreLib",
            "mscorlib",
            "netstandard",
            "System.Core",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Collections",
            "System.Text.Json",
            "System.Text.RegularExpressions",
            "System.ComponentModel.Annotations",
            "RoslynRules",
            "Microsoft.Extensions.Logging.Abstractions",
            // Roslyn compiler internals required for compilation
            "Microsoft.CodeAnalysis",
            "Microsoft.CodeAnalysis.CSharp",
            "Microsoft.CSharp",
            "Microsoft.CodeAnalysis.CSharp.Scripting"
        };

        /// <summary>
        /// Assemblies excluded from the reference set as a convenience so common risky
        /// APIs are not resolvable by accident. This is a best-effort convenience only,
        /// NOT a security control — many of these types are also reachable through the
        /// always-referenced core assemblies. See the type-level remarks.
        /// </summary>
        public static readonly string[] KnownDangerousAssemblies = new[]
        {
            "System.IO",
            "System.IO.FileSystem",
            "System.Diagnostics.Process",
            "System.Net.Http",
            "System.Net.Sockets",
            "System.Net.Security",
            "System.Security.Cryptography",
            "System.Reflection.Emit",
            "System.Runtime.Loader",
            "System.Data.SqlClient",
            "System.Data.OleDb",
            "System.Data.Odbc",
            "Microsoft.Win32.Registry"
        };

        /// <summary>
        /// Creates a reference provider with the default safe whitelist.
        /// </summary>
        public AssemblyReferenceProvider()
            : this(DefaultWhitelist)
        {
        }

        /// <summary>
        /// Creates a reference provider with a custom whitelist.
        /// </summary>
        /// <param name="whitelist">Assembly names to allow (partial match, case-insensitive).</param>
        public AssemblyReferenceProvider(IEnumerable<string> whitelist)
        {
            _allowedAssemblyNames = new HashSet<string>(
                whitelist.Select(n => n.ToLowerInvariant()));
            _blockedAssemblyNames = new HashSet<string>(
                KnownDangerousAssemblies.Select(n => n.ToLowerInvariant()));
        }

        /// <summary>
        /// Builds metadata references from the current AppDomain,
        /// filtered to only whitelisted assemblies.
        /// </summary>
        public List<MetadataReference> BuildReferences()
        {
            var references = new List<MetadataReference>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location))
                    continue;

                var name = asm.GetName().Name ?? "";
                var lowerName = name.ToLowerInvariant();

                // Skip known dangerous assemblies
                if (IsBlocked(lowerName))
                {
                    continue;
                }

                // Include if whitelisted
                if (IsAllowed(lowerName))
                {
                    references.Add(MetadataReference.CreateFromFile(asm.Location));
                }
            }

            return references;
        }

        /// <summary>
        /// Adds a custom assembly to the whitelist at runtime.
        /// </summary>
        public void AllowAssembly(string assemblyName)
        {
            _allowedAssemblyNames.Add(assemblyName.ToLowerInvariant());
        }

        /// <summary>
        /// Explicitly blocks an assembly.
        /// </summary>
        public void BlockAssembly(string assemblyName)
        {
            _blockedAssemblyNames.Add(assemblyName.ToLowerInvariant());
        }

        private bool IsAllowed(string lowerName)
        {
            return _allowedAssemblyNames.Any(w => lowerName.Contains(w));
        }

        private bool IsBlocked(string lowerName)
        {
            return _blockedAssemblyNames.Any(b => lowerName.Contains(b));
        }
    }
}
