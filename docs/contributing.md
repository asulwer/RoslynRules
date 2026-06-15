---
layout: default
title: Contributing
nav_order: 9
---

# Contributing to RoslynRules

Thank you for your interest in contributing! This project welcomes bug reports, feature requests, documentation improvements, and code contributions.

---

## Quick Start

1. **Fork** the repository on GitHub
2. **Clone** your fork locally
3. **Build** the solution: `dotnet build`
4. **Run tests**: `dotnet test`
5. **Create a branch**: `git checkout -b feature/my-change`
6. **Commit** with a descriptive message (see format below)
7. **Push** and open a Pull Request

---

## Development Setup

### Requirements

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- PowerShell 7+ (for scripts in `/scripts`)

### Build

```bash
dotnet build --configuration Release
```

### Test

```bash
# All tests
dotnet test

# With diagnostics
dotnet test --logger "console;verbosity=detailed"

# Specific project
dotnet test RoslynRules.Tests
```

### Benchmarks

```bash
dotnet run --project RoslynRules.Benchmarks --configuration Release
```

---

## Project Structure

| Directory | Contents |
|-----------|----------|
| `RoslynRules/` | Core library |
| `RoslynRules.Json/` | JSON serialization |
| `RoslynRules.Tests/` | Unit and integration tests |
| `RoslynRules.Benchmarks/` | BenchmarkDotNet suite |
| `RoslynRules.Demo/` | Demo application |
| `RoslynRules.Demo.Tests/` | Demo integration tests |
| `docs/` | GitHub Pages documentation |
| `scripts/` | Local automation scripts (gitignored) |

---

## Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

[optional body]

[optional footer: Fixes #123]
```

Types: `feat`, `fix`, `docs`, `test`, `perf`, `refactor`, `chore`

Examples:
```
feat(compiler): add async delegate compilation
fix(execution): handle null parameters in RuleContext
docs(api): document RuleMetrics properties
test(aot): add trim compatibility tests
```

---

## Code Style

- Follow existing formatting (4-space indentation)
- Add XML documentation for all public APIs
- Keep methods focused and under 50 lines where possible
- Use `sealed` for classes not designed for inheritance
- Run `dotnet format` before committing

---

## Testing Guidelines

### Unit Tests

- Name format: `MethodName_Scenario_ExpectedResult`
- Use xUnit facts and theories
- Assert with `FluentAssertions` where available

### Integration Tests

- Test real compilation and execution
- Use `TestCompiler.Instance` to share `ExpressionCompiler` across tests
- Clean up with `compiler.Unload()` where appropriate

### AOT Tests

- Mark AOT-requiring tests with `[RequiresUnreferencedCode]`
- Place in `RoslynRules.Tests/AotCompatibility/`

---

## Documentation

When adding or changing features:

1. Update XML docs in source code
2. Add/update markdown docs in `docs/`
3. Include Jekyll frontmatter for page navigation
4. Cross-link related pages
5. Update `CHANGELOG.md`

---

## Pull Request Process

1. Ensure all tests pass locally
2. Update docs for any API changes
3. Add CHANGELOG entry
4. Keep PRs focused — one concern per PR
5. Respond to review feedback promptly

---

## Reporting Issues

When reporting bugs, include:

- .NET version (`dotnet --version`)
- RoslynRules version
- Minimal reproduction code
- Expected vs actual behavior
- Full exception stack trace (if applicable)

---

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
