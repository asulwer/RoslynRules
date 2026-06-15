# Changelog

All notable changes to RoslynRules will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2026.6.3] - 2026-06-03

### Added

- Multi-parameter rule support — rules now accept up to 16 parameters directly (no wrapper struct needed)
- `CompiledMultiParamDelegate` and `CompiledAsyncMultiParamDelegate` for multi-parameter invocation
- Static `Rule.ValidateSemantics(string, Type)` and `Rule.ValidateSemantics(string, string)` overloads for expression validation without creating a Rule instance
- Type alias resolution (`int`, `string`, `bool`, etc.) in static `ValidateSemantics`
- `Rule.TryGetValue<T>()` with `[MaybeNullWhen(false)]` attribute for null-safe value retrieval
- `RuleCache.TryGet()` with `[NotNullWhen(true)]` attribute for null-safe cache access
- ExpressionAssemblyLoadContext unload verification tests (8 stress/integration tests)
- Demo integration tests in `RoslynRules.Tests/Integration/` (JSON round-trip, template instantiation, workflow vs batch comparison, multi-parameter)
- Advanced documentation guides:
  - `docs/security.md` — AssemblyReferenceProvider hardening, sandboxing, input validation, security checklist
  - `docs/performance-tuning.md` — CacheDuration, Timeout, maxCompilesBeforeRecycle tuning, execution strategy selection
- Dependency chaining example in `docs/examples/index.md` — `DependsOnRuleId` + `RuleContext.GetValue<T>` pattern
- BenchmarkDotNet benchmarks for compile, execute, and cache performance
- Rule templates (`RuleTemplate`) with placeholder-based expression substitution
- `PlaceholderKind` enum supporting Type, Identifier, and Value placeholders
- Result caching (`Rule.CacheDuration`) with automatic expiration
- `Rule.ClearCache()` for manual cache invalidation
- Security policy (`SECURITY.md`) documenting expression injection risks
- Contributing guidelines (`CONTRIBUTING.md`)

### Changed

- `Workflow.Rules` is now `IReadOnlyList<Rule>` (via `ReadOnlyCollection`) after compilation; mutation throws `NotSupportedException`
- `Workflow.Compile()` sets `_isCompiled = true` after compiling all rules, locking the rules list
- Tests reorganized into categorized folders (`Compiler/`, `Core/`, `Execution/`, `Integration/`, `Models/`, `WorkflowTests/`)
- `RulesException` documented correctly as inheriting from `Exception` (not `InvalidOperationException`)

### Fixed

- `aot.yml` workflow missing explicit `permissions: contents: read` (GitHub security alert)
- `ExpressionCompiler` cache race condition (now uses `ConcurrentDictionary` with atomic `GetOrAdd`)
- `Workflow.ExecuteParallel` now respects dependency chains via `GetExecutionOrder()`
- Sync timeout implementation refactored to avoid thread-pool starvation
- `DelegateFactory` null-forgiving operators replaced with explicit null checks
- `JsonRuleLoader` reflection-based ID restoration replaced with `[JsonInclude]` attributes
- Duplicate "Result Caching" section removed from README
- Removed lingering EF Core references (`virtual` keyword, EF comments) from `Workflow.cs` and `Rule.cs`
- Removed `[NotMapped]`, `[Key]`, `[JsonInclude]`, `[JsonIgnore]` attributes from core models
- `Customer.cs` in Demo no longer references `System.ComponentModel.DataAnnotations`

## [2026.6.2-2] - 2026-06-02

### Changed

- Version bump for package release

## [2026.6.2-1] - 2026-06-02

### Changed

- `release.yml` CI workflow updated for independent package versioning

### Fixed

- NuGet package creation excludes Demo and Tests projects

## [2026.6.2] - 2026-06-02

### Added

- `RoslynRules.Json` extension package with `JsonRuleLoader`
- `RoslynRules.EntityFrameworkCore` extension package (renamed from `RoslynRules.EFCore`)
- `RuleParameter.ForCompile()` and `ForExecute()` factory methods
- Built-in rule predicates library (`RulePredicates`)
- Rule composition/template system (`RuleTemplate`, `PlaceholderKind`)
- Result caching (`Rule.CacheDuration`) with automatic expiration
- `Rule.ClearCache()` for manual cache invalidation
- `IRuleEngine` abstraction for DI and testing
- Lifecycle events (`OnRuleExecuting`, `OnRuleExecuted`)
- Per-rule timeout with `RuleTimeoutException`
- `ValidateSemantics()` for compile-time semantic validation
- `ValidateAll()` — non-throwing validation returning all errors
- `TryGetValue<T>()` on `RuleContext`
- Source Link support for debugging
- Multi-targeting `net8.0` and `net9.0`
- Comprehensive demo with 12 feature showcases

### Changed

- `RuleResult` converted from class to readonly record struct
- JSON serialization uses direct model serialization (no DTOs)
- Tests reorganized into categorized folders
- `RulesException` documented correctly as inheriting from `Exception`
- Documentation restructured with Jekyll-compatible navigation

### Fixed

- `ExpressionCompiler` cache race condition
- `RuleCache` lazy cleanup race condition
- `Workflow.ExecuteParallelAsync` dependency-level batching
- Cooperative cancellation in sync and async execution
- `CacheKeyBuilder` handling of reference types and collections
- `RulePredicates` parameter name validation
- Actions compiled as void to prevent assignment result leak
- Roslyn syntax tree for await detection
- `DependsOnRuleId` validation in `Rule.Validate()`
- `RuleContext` thread-safety with `ConcurrentDictionary`
- CompiledDelegate null-safety

## [2026.6.1] - 2026-06-01

### Added

- `AssemblyReferenceProvider` sandboxing for expression compilation
- Collectible `AssemblyLoadContext` for expression assemblies
- Assembly memory leak prevention with `Unload()` and `maxCompilesBeforeRecycle`
- AOT/trimming annotations (`RequiresUnreferencedCode`, `RequiresDynamicCode`)
- Comprehensive benchmarks (RuleResult struct vs class, compile, execute, cache)
- `.editorconfig` for consistent formatting
- Security policy (`SECURITY.md`)
- Contributing guidelines (`CONTRIBUTING.md`)

### Changed

- `Rule` and `RuleBatch` sealed (Workflow excluded for EF Core compatibility at the time)
- Target framework updated: `net8.0` and `net9.0`, dropped `netstandard2.1`
- Language version bumped to C# 12.0
- `TreatWarningsAsErrors` enabled

### Fixed

- Async deadlock prevention with `ConfigureAwait(false)`

## [2026.5.31-1] - 2026-05-31

### Fixed

- NuNet push `--skip-duplicate` flag to handle already-published packages

## [2026.5.31] - 2026-05-31

### Added

- Initial release with rule engine core
- `Rule`, `Workflow`, and `RuleBatch` models
- Roslyn-based expression compilation (`ExpressionCompiler`)
- Sync and async rule execution
- Child rule nesting with bottom-up evaluation
- Dependency chains via `DependsOnRuleId`
- Action expressions (`Rule.Action`)
- Rule validation (`Validate`, `ValidateAll`)
- Circular reference detection
- Timeout support (`Rule.Timeout`)
- Lifecycle events (`OnRuleExecuting`, `OnRuleExecuted`)
- Logging integration (`Microsoft.Extensions.Logging`)
- JSON serialization (`JsonRuleLoader`)
- Testing framework (`RuleTest`, `RuleTestSuite`)
- ExpandoObject parameter support
- Partial results (`Workflow.ExecuteBufferedAsync`)
- `IRuleEngine` abstraction

---

## Release Notes Template

```
## [X.Y.Z] - YYYY-MM-DD

### Added
- New features

### Changed
- Changes in existing functionality

### Deprecated
- Soon-to-be removed features

### Removed
- Now removed features

### Fixed
- Bug fixes

### Security
- Security-related changes
```
