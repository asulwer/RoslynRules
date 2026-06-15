using FluentAssertions;
using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Linq;
using Xunit;

namespace RoslynRules.Tests.Compiler
{
    /// <summary>
    /// Security tests for AssemblyReferenceProvider sandboxing.
    /// Verifies that dangerous assemblies are excluded and expressions
    /// cannot access forbidden namespaces like System.IO, System.Net, etc.
    /// </summary>
    public class AssemblyReferenceProviderTests
    {
        [Fact]
        public void DefaultWhitelist_Contains_CoreSafeAssemblies()
        {
            var whitelist = AssemblyReferenceProvider.DefaultWhitelist;

            whitelist.Should().Contain("System.Runtime");
            whitelist.Should().Contain("System.Linq");
            whitelist.Should().Contain("System.Collections");
            whitelist.Should().Contain("RoslynRules");
        }

        [Fact]
        public void KnownDangerousAssemblies_Contains_SystemIO()
        {
            var dangerous = AssemblyReferenceProvider.KnownDangerousAssemblies;

            dangerous.Should().Contain("System.IO");
            dangerous.Should().Contain("System.IO.FileSystem");
        }

        [Fact]
        public void KnownDangerousAssemblies_Contains_SystemNet()
        {
            var dangerous = AssemblyReferenceProvider.KnownDangerousAssemblies;

            dangerous.Should().Contain("System.Net.Http");
            dangerous.Should().Contain("System.Net.Sockets");
            dangerous.Should().Contain("System.Net.Security");
        }

        [Fact]
        public void KnownDangerousAssemblies_Contains_SystemDiagnostics()
        {
            var dangerous = AssemblyReferenceProvider.KnownDangerousAssemblies;

            dangerous.Should().Contain("System.Diagnostics.Process");
        }

        [Fact]
        public void KnownDangerousAssemblies_Contains_SystemSecurity()
        {
            var dangerous = AssemblyReferenceProvider.KnownDangerousAssemblies;

            dangerous.Should().Contain("System.Security.Cryptography");
            dangerous.Should().Contain("System.Reflection.Emit");
            dangerous.Should().Contain("System.Runtime.Loader");
        }

        [Fact]
        public void KnownDangerousAssemblies_Contains_SystemData()
        {
            var dangerous = AssemblyReferenceProvider.KnownDangerousAssemblies;

            dangerous.Should().Contain("System.Data.SqlClient");
            dangerous.Should().Contain("System.Data.OleDb");
            dangerous.Should().Contain("System.Data.Odbc");
        }

        [Fact]
        public void KnownDangerousAssemblies_Contains_Win32Registry()
        {
            var dangerous = AssemblyReferenceProvider.KnownDangerousAssemblies;

            dangerous.Should().Contain("Microsoft.Win32.Registry");
        }

        [Fact]
        public void BuildReferences_WithDefaultProvider_Excludes_SystemIO()
        {
            var provider = new AssemblyReferenceProvider();
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "");
            locations.Should().NotContain(l => l.Contains("System.IO") && !l.Contains("System.IO.Pipelines") && !l.Contains("System.IO.Compression"));
        }

        [Fact]
        public void BuildReferences_WithDefaultProvider_Excludes_SystemNet()
        {
            var provider = new AssemblyReferenceProvider();
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "");
            locations.Should().NotContain(l => l.Contains("System.Net.Http") || l.Contains("System.Net.Sockets"));
        }

        [Fact]
        public void BuildReferences_WithDefaultProvider_Excludes_SystemDiagnosticsProcess()
        {
            var provider = new AssemblyReferenceProvider();
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "");
            locations.Should().NotContain(l => l.Contains("System.Diagnostics.Process"));
        }

        [Fact]
        public void BuildReferences_WithDefaultProvider_Includes_SystemRuntime()
        {
            var provider = new AssemblyReferenceProvider();
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "");
            locations.Should().Contain(l => l.Contains("System.Runtime"));
        }

        [Fact]
        public void BuildReferences_WithDefaultProvider_Includes_SystemLinq()
        {
            var provider = new AssemblyReferenceProvider();
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "");
            locations.Should().Contain(l => l.Contains("System.Linq"));
        }

        [Fact]
        public void BuildReferences_WithCustomWhitelist_OnlyIncludesWhitelisted()
        {
            var provider = new AssemblyReferenceProvider(new[] { "System.Runtime" });
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "").ToList();
            locations.Should().Contain(l => l.Contains("System.Runtime"));
            locations.Should().NotContain(l => l.Contains("System.Linq"));
        }

        [Fact]
        public void AllowAssembly_Runtime_AddsToWhitelist()
        {
            var provider = new AssemblyReferenceProvider(new[] { "System.Runtime" });
            provider.AllowAssembly("System.Linq");
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "").ToList();
            locations.Should().Contain(l => l.Contains("System.Linq"));
        }

        [Fact]
        public void BlockAssembly_Runtime_AddsToBlockedList()
        {
            var provider = new AssemblyReferenceProvider();
            provider.BlockAssembly("System.Runtime");
            var references = provider.BuildReferences();

            var locations = references.Select(r => r.Display ?? "").ToList();
            locations.Should().NotContain(l => l.Contains("System.Runtime"));
        }

        [Fact]
        public void Compile_WithDefaultProvider_FileDeleteExpression_Fails()
        {
            var compiler = new ExpressionCompiler();

            Action act = () =>
            {
                var del = compiler.Compile<Func<int, bool>>(
                    "System.IO.File.Delete(\"test.txt\")",
                    new[] { "x" },
                    referenceProvider: new AssemblyReferenceProvider());
                del.Invoke(0);
            };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Compile_WithDefaultProvider_ProcessStartExpression_Fails()
        {
            var compiler = new ExpressionCompiler();

            Action act = () =>
            {
                var del = compiler.Compile<Func<int, bool>>(
                    "System.Diagnostics.Process.Start(\"notepad.exe\") != null",
                    new[] { "x" },
                    referenceProvider: new AssemblyReferenceProvider());
                del.Invoke(0);
            };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Compile_WithDefaultProvider_HttpClientExpression_Fails()
        {
            var compiler = new ExpressionCompiler();

            Action act = () =>
            {
                var del = compiler.Compile<Func<int, bool>>(
                    "new System.Net.Http.HttpClient() != null",
                    new[] { "x" },
                    referenceProvider: new AssemblyReferenceProvider());
                del.Invoke(0);
            };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Compile_WithCustomBlockedAssembly_BlocksExplicitly()
        {
            var compiler = new ExpressionCompiler();
            var provider = new AssemblyReferenceProvider();
            provider.BlockAssembly("System.Text.Json");

            Action act = () =>
            {
                var del = compiler.Compile<Func<int, bool>>(
                    "System.Text.Json.JsonSerializer.Serialize(1) != null",
                    new[] { "x" },
                    referenceProvider: provider);
                del.Invoke(0);
            };

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void Compile_WithExplicitAllowance_CanAccessAllowedAssembly()
        {
            var compiler = new ExpressionCompiler();
            var provider = new AssemblyReferenceProvider();
            provider.AllowAssembly("RoslynRules.Tests");

            // Use a type from the test assembly that has a simple static method
            var del = compiler.Compile<Func<int, bool>>(
                "typeof(RoslynRules.Tests.Compiler.AssemblyReferenceProviderTests) != null",
                new[] { "x" },
                referenceProvider: provider);

            var result = del.Invoke(42);
            result.Should().BeTrue();
        }

        [Fact]
        public void Compile_SafeExpression_WorksWithDefaultProvider()
        {
            var compiler = new ExpressionCompiler();

            var del = compiler.Compile<Func<int, bool>>(
                "x > 3",
                new[] { "x" },
                referenceProvider: new AssemblyReferenceProvider());

            var result = del.Invoke(5);
            result.Should().BeTrue();
        }

        [Fact]
        public void Compile_SystemLinqExpression_WorksWithDefaultProvider()
        {
            var compiler = new ExpressionCompiler();
            var provider = new AssemblyReferenceProvider();

            var del = compiler.Compile<Func<int, bool>>(
                "System.Linq.Enumerable.Range(0, x).Any()",
                new[] { "x" },
                referenceProvider: provider);

            var result = del.Invoke(5);
            result.Should().BeTrue();
        }
    }
}
