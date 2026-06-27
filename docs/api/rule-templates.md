---
layout: default
title: Rule Templates
parent: API Reference
nav_order: 9
---

[← Back to API Reference](../api-reference.md)

# Rule Templates

Reusable rule templates with placeholders for type-safe rule generation.

```csharp
using RoslynRules.Templates;
```

---

## RuleTemplate

```csharp
public class RuleTemplate
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Description` | `string` | Template description |
| `Expression` | `string` | Expression template with `{placeholder}` syntax |
| `Action` | `string` | Action template (optional) |
| `Placeholders` | `Dictionary<string, PlaceholderKind>` | Defined placeholders |

### Methods

#### `Instantiate(Dictionary<string, object>, ExpressionCompiler, RuleParameter[], string[], AssemblyReferenceProvider?)`

Creates a compiled `Rule` from the template with placeholder values substituted. The trailing
`AssemblyReferenceProvider? referenceProvider = null` argument is optional and controls
compilation sandboxing.

```csharp
var template = new RuleTemplate
{
    Description = "Age threshold",
    Expression = "customer.Age >= {minAge}"
};
template.Placeholders.Add("minAge", PlaceholderKind.Value);

var values = new Dictionary<string, object> { ["minAge"] = 18 };
var rule = template.Instantiate(values, compiler, parameters, Array.Empty<string>());
```

#### `ExtractPlaceholders()`

Instance method that extracts placeholder names from this template's `Expression` (no
parameters). Returns `IReadOnlyList<string>`.

```csharp
var template = new RuleTemplate
{
    Expression = "customer.Age >= {minAge} && customer.Score >= {minScore}"
};

IReadOnlyList<string> names = template.ExtractPlaceholders();
// ["minAge", "minScore"]
```

---

## PlaceholderKind

| Kind | Substitution | Example |
|------|------------|---------|
| `Value` | Quoted/escaped value | `"Alice"`, `42`, `true` |
| `Type` | Unquoted type name | `Customer`, `System.String` |
| `Identifier` | Raw text | `Name`, `Age` |

---

## Related

- [Rule](rule.md) — Produced by template instantiation
- [ExpressionCompiler](expressioncompiler.md) — Required for instantiation
