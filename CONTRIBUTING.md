# Contributing to RoslynRules

Thank you for your interest in contributing! This document outlines the process and guidelines.

## Getting Started

1. Fork the repository
2. Create a feature branch from `master`
3. Make your changes
4. Add or update tests
5. Ensure all tests pass (`dotnet test`)
6. Submit a pull request

## Development Setup

Requirements:
- .NET 9.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider

```bash
git clone https://github.com/asulwer/RoslynRules.git
cd RoslynRules
dotnet build
dotnet test
```

## Pull Request Process

1. Update the README.md or docs/ if your change affects public API or behavior
2. Update or add tests for any new functionality
3. Ensure the build passes (`dotnet build`) — warnings are treated as errors by default
4. Link any related issues in your PR description
5. Request review from maintainers

## Code Style

- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Add XML documentation for public APIs
- Follow existing naming conventions
- Keep methods focused and concise

## Testing

- All new features must include unit tests
- Aim for meaningful assertions, not just coverage
- Test edge cases: nulls, empty collections, exceptions
- Async code must have async tests

## Reporting Bugs

When reporting bugs, please include:
- Minimal reproduction steps
- Expected vs actual behavior
- .NET version and OS
- Any relevant error messages or stack traces

## Feature Requests

Open an issue with the `enhancement` label. Describe:
- The problem you're trying to solve
- Your proposed solution
- Any alternatives considered

## Questions?

Feel free to open a discussion issue or reach out to maintainers.
