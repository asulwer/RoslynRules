using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace RoslynRules.Compiler
{
    /// <summary>
    /// Public entry point for compiling C# expression strings into typed delegates.
    /// Orchestrates CodeGenerator → AssemblyCompiler → DelegateFactory.
    /// Results are cached for reuse.
    /// </summary>
    public class ExpressionCompiler
    {
        // Values are Lazy so that, under concurrent GetOrAdd for the same key, the expensive
        // compilation runs exactly once (ConcurrentDictionary.GetOrAdd may otherwise invoke its
        // factory multiple times, loading duplicate assemblies into the load context).
        private readonly ConcurrentDictionary<string, Lazy<Delegate>> _cache = new();
        private readonly int _maxCompilesBeforeRecycle;
        private readonly object _lock = new();

        // Lifetime diagnostic total. Reset only by an explicit Unload(), never by auto-recycle.
        private int _compileCount = 0;

        // Drives the recycle threshold; reset on every recycle (auto or explicit). Separating this
        // from _compileCount fixes a latent bug where, once the threshold was crossed, the context
        // recycled on every subsequent compile because the single counter never reset.
        private int _compilesSinceRecycle = 0;

        private ExpressionAssemblyLoadContext _context;

        /// <summary>
        /// Creates a new ExpressionCompiler with optional compile limit.
        /// </summary>
        /// <param name="maxCompilesBeforeRecycle">
        /// Maximum unique compilations before the internal AssemblyLoadContext is unloaded
        /// and a new one created. Set to 0 for no limit (default: 1000).
        /// Lower this in memory-constrained environments.
        /// </param>
        public ExpressionCompiler(int maxCompilesBeforeRecycle = 1000)
        {
            _maxCompilesBeforeRecycle = maxCompilesBeforeRecycle;
            _context = CreateContext();
        }

        /// <summary>
        /// Compiles a C# expression string into a strongly-typed delegate.
        /// Results are cached; subsequent calls with the same signature return the cached delegate.
        /// </summary>
        /// <typeref name="TDelegate">The delegate type, e.g. Func&lt;Customer, bool&gt;.</typeref>
        /// <param name="expression">The C# expression body.</param>
        /// <param name="parameterNames">Ordered parameter names matching the delegate signature.</param>
        /// <param name="additionalNamespaces">Optional extra using namespaces (e.g. "Demo.Models").</param>
        /// <param name="referenceProvider">Optional custom assembly reference provider for sandboxing. Defaults to safe whitelist.</param>
        /// <returns>A typed delegate that evaluates the expression.</returns>
        /// <exception cref="InvalidOperationException">Thrown when expression compilation fails.</exception>
        [RequiresUnreferencedCode("RoslynRules uses reflection to inspect delegate signatures (GetMethod, GetParameters). This code may not work correctly with trimming or AOT.")]
        public TDelegate Compile<TDelegate>(
            string expression,
            string[] parameterNames,
            string[]? additionalNamespaces = null,
            AssemblyReferenceProvider? referenceProvider = null) where TDelegate : Delegate
        {
            AotCompatibility.ThrowIfAot("ExpressionCompiler.Compile<TDelegate>");

            // STEP 1: Build a unique cache key. The reference provider is part of the key so
            // that the same expression compiled under different providers is not shared.
            var cacheKey = BuildCacheKey<TDelegate>(expression, parameterNames, additionalNamespaces, referenceProvider);

            // The Lazy guarantees CompileInternal runs once per key even if GetOrAdd's factory
            // is raced; the winning Lazy's Value performs the single compilation.
            var lazy = _cache.GetOrAdd(cacheKey, _ => new Lazy<Delegate>(
                () => CompileInternal<TDelegate>(expression, parameterNames, additionalNamespaces, referenceProvider)));

            try
            {
                return (TDelegate)lazy.Value;
            }
            catch
            {
                // A Lazy caches thrown exceptions, so a failed compilation would otherwise be
                // returned forever. Remove this exact entry so a later call can retry.
                ((System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Lazy<Delegate>>>)_cache)
                    .Remove(new System.Collections.Generic.KeyValuePair<string, Lazy<Delegate>>(cacheKey, lazy));
                throw;
            }
        }

        private TDelegate CompileInternal<TDelegate>(
            string expression,
            string[] parameterNames,
            string[]? additionalNamespaces,
            AssemblyReferenceProvider? referenceProvider) where TDelegate : Delegate
        {
            // Check compile limit and recycle ALC if needed.
            lock (_lock)
            {
                if (_maxCompilesBeforeRecycle > 0 && _compilesSinceRecycle >= _maxCompilesBeforeRecycle)
                {
                    RecycleContext();
                }
                _compileCount++;
                _compilesSinceRecycle++;
            }

            // STEP 2: Reflect the delegate type to discover its signature.
            var delegateType = typeof(TDelegate);
            var invokeMethod = delegateType.GetMethod("Invoke")!;
            var returnType = invokeMethod.ReturnType;
            var parameters = invokeMethod.GetParameters();
            var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

            // STEP 3: Generate C# source code.
            var code = CodeGenerator.Generate(
                expression,
                returnType,
                parameterNames,
                parameterTypes,
                additionalNamespaces);

            // STEP 4: Compile source code into raw assembly bytes with sandboxing.
            var assemblyBytes = AssemblyCompiler.Compile(code, referenceProvider);

            // STEP 5: Load assembly into collectible context and create a typed delegate.
            lock (_lock)
            {
                return (TDelegate)DelegateFactory.CreateDelegate(assemblyBytes, delegateType, _context);
            }
        }

        /// <summary>
        /// Forces immediate unload of the current AssemblyLoadContext and clears the delegate cache.
        /// Use this when memory pressure is detected or before disposing the compiler.
        /// </summary>
        public void Unload()
        {
            lock (_lock)
            {
                RecycleContext();
                // Explicit unload also resets the lifetime diagnostic counter.
                _compileCount = 0;
            }
        }

        /// <summary>
        /// Returns the number of unique compilations performed by this compiler.
        /// </summary>
        public int CompileCount => _compileCount;

        /// <summary>
        /// Returns the current AssemblyLoadContext name (for diagnostics).
        /// </summary>
        public string CurrentContextName
        {
            get
            {
                lock (_lock)
                {
                    return _context.ToString();
                }
            }
        }

        private void RecycleContext()
        {
            // Clear cached delegates FIRST. Each cached delegate holds a strong reference to an
            // assembly loaded in the outgoing context; leaving them in the cache would pin that
            // context and prevent Unload from ever reclaiming its memory. Dropping them lets the
            // old ALC be collected — expressions transparently recompile on next use.
            _cache.Clear();
            _compilesSinceRecycle = 0;

            var oldContext = _context;
            _context = CreateContext();

            // The old ALC is now eligible for collection once any in-flight delegate references
            // are released; the actual unload completes on a subsequent GC cycle.
            oldContext.Unload();
        }

        private static ExpressionAssemblyLoadContext CreateContext()
        {
            return new ExpressionAssemblyLoadContext(Guid.NewGuid().ToString("N")[..8]);
        }

        /// <summary>
        /// Builds a unique cache key combining delegate type, expression, parameters, and namespaces.
        /// </summary>
        private static string BuildCacheKey<TDelegate>(
            string expression,
            string[] parameterNames,
            string[]? additionalNamespaces,
            AssemblyReferenceProvider? referenceProvider)
        {
            // A different provider changes which references (and thus which resolutions) are
            // available, so it must not share a compiled delegate. Providers do not define value
            // equality, so identity is used: the same provider instance reuses its cache entry.
            var providerKey = referenceProvider is null
                ? "default"
                : referenceProvider.GetHashCode().ToString();

            var key = $"{typeof(TDelegate).FullName}:{expression}:{string.Join(",", parameterNames)}:{providerKey}";

            if (additionalNamespaces != null && additionalNamespaces.Length > 0)
            {
                key += $":{string.Join(",", additionalNamespaces)}";
            }

            return key;
        }
    }
}
