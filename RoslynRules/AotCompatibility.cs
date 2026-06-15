using RoslynRules.Exceptions;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RoslynRules;

/// <summary>
/// Provides runtime detection of AOT/trimming environments and guards
/// for JIT-only APIs. All compilation paths should call
/// <see cref="ThrowIfAot(string)"/> before invoking Roslyn or reflection.
/// </summary>
public static class AotCompatibility
{
    /// <summary>
    /// Cached result of AOT detection. True when the runtime is running
    /// in an AOT-published or aggressively-trimmed mode where reflection
    /// and dynamic code generation are unavailable.
    /// </summary>
    public static bool IsAot { get; } = DetectAot();

    /// <summary>
    /// Throws <see cref="AotCompatibilityException"/> when running in AOT mode.
    /// Call this at the entry point of every JIT-only API.
    /// </summary>    /// <param name="apiName">Name of the API that requires JIT (used in the exception message).</param>
    public static void ThrowIfAot(string apiName)
    {
        if (IsAot)
            throw new AotCompatibilityException(apiName);
    }

    /// <summary>
    /// Detects AOT/trimming at runtime using multiple heuristics.
    /// Returns true if dynamic compilation and reflection-emit are unavailable.
    /// </summary>
    private static bool DetectAot()
    {
        // Heuristic 1: RuntimeFeature.IsDynamicCodeSupported (introduced in .NET 6)
        // This is the most reliable signal for Native AOT and trimming.
        try
        {
            if (!RuntimeFeature.IsDynamicCodeSupported)
                return true;
        }
        catch
        {
            // RuntimeFeature may not exist on very old runtimes — fall through.
        }

        // Heuristic 2: Check if AssemblyBuilder (reflection emit) is functional.
        // In AOT, the type exists but instantiation throws.
        try
        {
            var assemblyBuilderType = Type.GetType("System.Reflection.Emit.AssemblyBuilder, System.Private.CoreLib");
            if (assemblyBuilderType != null)
            {
                // Try to invoke DefineDynamicAssembly — this throws in AOT
                var defineMethod = assemblyBuilderType.GetMethod(
                    "DefineDynamicAssembly",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(AssemblyName) },
                    null);

                if (defineMethod != null)
                {
                    defineMethod.Invoke(null, new object[] { new AssemblyName("AotProbe") });
                }
            }
        }
        catch (TargetInvocationException tie) when (tie.InnerException is PlatformNotSupportedException or InvalidOperationException)
        {
            return true;
        }
        catch (PlatformNotSupportedException)
        {
            return true;
        }
        catch
        {
            // Any other exception is inconclusive — keep checking.
        }

        // Heuristic 3: Check RuntimeFeature for reflection emit.
        try
        {
            var reflectionEmitSupported = typeof(RuntimeFeature)
                .GetProperty("IsDynamicCodeCompiled")
                ?.GetValue(null);

            if (reflectionEmitSupported is false)
                return true;
        }
        catch
        {
            // Inconclusive.
        }

        return false;
    }
}
