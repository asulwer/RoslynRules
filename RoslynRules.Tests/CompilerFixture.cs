namespace RoslynRules.Tests
{
    /// <summary>
    /// Shared test compiler instance. Eliminates per-test Roslyn warmup overhead.
    /// </summary>
    public static class TestCompiler
    {
        /// <summary>
        /// Lazily-initialized shared ExpressionCompiler instance.
        /// Thread-safe; created on first access.
        /// </summary>
        public static global::RoslynRules.Compiler.ExpressionCompiler Instance { get; } = new();
    }
}
