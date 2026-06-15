---
layout: default
title: Rule Templates
parent: API Reference
nav_order: 9
---

[← Back to API Reference](api-reference.md)

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

#### `Instantiate(Dictionary<string, object>, ExpressionCompiler, RuleParameter[], string[]?)`

Creates a compiled `Rule` from the template with placeholder values substituted.

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

#### `ExtractPlaceholders(string)`

Extracts placeholder names from an expression string.

```csharp
var names = RuleTemplate.ExtractPlaceholders("customer.Age >= {minAge} && customer.Score >= {minScore}");
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
