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
        private readonly ConcurrentDictionary<string, Delegate> _cache = new();
        private readonly int _maxCompilesBeforeRecycle;
        private readonly object _lock = new();
        private int _compileCount = 0;
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

            // STEP 1: Build a unique cache key.
            var cacheKey = BuildCacheKey<TDelegate>(expression, parameterNames, additionalNamespaces);

            // Atomic GetOrAdd — compilation happens inside the factory, so only one thread compiles.
            var del = _cache.GetOrAdd(cacheKey, key => CompileInternal<TDelegate>(expression, parameterNames, additionalNamespaces, referenceProvider));
            return (TDelegate)del;
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
                if (_maxCompilesBeforeRecycle > 0 && _compileCount >= _maxCompilesBeforeRecycle)
                {
                    RecycleContext();
                }
                _compileCount++;
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
                _cache.Clear();
                RecycleContext();
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
            // Unload the current context. The ALC becomes collectible once all
            // delegates loaded from it are no longer referenced. Since the cache
            // holds strong references to delegates, we must clear it first.
            var oldContext = _context;
            _context = CreateContext();

            // Note: The old ALC is now eligible for GC collection. The actual unload
            // happens on the next GC cycle after all references are released.
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
            string[]? additionalNamespaces)
        {
            var key = $"{typeof(TDelegate).FullName}:{expression}:{string.Join(",", parameterNames)}";

            if (additionalNamespaces != null && additionalNamespaces.Length > 0)
            {
                key += $":{string.Join(",", additionalNamespaces)}";
            }

            return key;
        }
    }
}
