---
layout: default
title: Rule Localization
parent: API Reference
nav_order: 12
---

# Rule Localization (i18n)

RoslynRules supports localized rule descriptions via `DescriptionKey` and `IRuleDescriptionProvider`. This lets you separate display text from rule logic and support multiple languages without changing your rule definitions.

## Overview

| Before | After |
|--------|-------|
| `Description = "Adult check"` | `DescriptionKey = "rule.adultCheck"` |
| Hardcoded English | Any language at runtime |
| Mutate rule to change text | Swap the provider implementation |

## IRuleDescriptionProvider

Implement this interface to integrate with your app's localization strategy:

```csharp
public interface IRuleDescriptionProvider
{
    string? GetDescription(string key, string? culture = null);
}
```

**No dependencies.** Use resx files, JSON dictionaries, database lookups, or any strategy you prefer.

## Quick Start

### 1. Define a provider

```csharp
public class JsonDescriptionProvider : IRuleDescriptionProvider
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    public JsonDescriptionProvider(string jsonFilePath)
    {
        var json = File.ReadAllText(jsonFilePath);
        _translations = JsonSerializer.Deserialize<...>(json);
    }

    public string? GetDescription(string key, string? culture = null)
    {
        if (_translations.TryGetValue(key, out var cultureMap))
        {
            if (culture != null && cultureMap.TryGetValue(culture, out var localized))
                return localized;

            // Fallback to first available culture
            return cultureMap.Values.FirstOrDefault();
        }
        return null;
    }
}
```

### 2. Create rules with keys

```csharp
var rule = new Rule
{
    Description = "Adult check",        // Fallback text
    DescriptionKey = "rule.adultCheck", // Localization key
    DescriptionProvider = provider,   // Your implementation
    Expression = "customer.Age >= 18",
    IsActive = true
};
```

### 3. Get localized text

```csharp
// Uses provider if available, falls back to Description
var text = rule.GetLocalizedDescription();        // Default culture
var french = rule.GetLocalizedDescription("fr-FR"); // Specific culture
```

### 4. Execution results include localized descriptions

```csharp
var result = rule.Execute(parameters);
Console.WriteLine(result.RuleDescription); // "Vérification adulte" (if fr-FR)
```

## JSON Translation File Example

```json
{
  "rule.adultCheck": {
    "en-US": "Adult check",
    "fr-FR": "Vérification adulte",
    "es-ES": "Verificación de adultos"
  },
  "rule.vipDiscount": {
    "en-US": "VIP discount applied",
    "fr-FR": "Remise VIP appliquée"
  }
}
```

## Fallback Chain

`GetLocalizedDescription()` follows this priority:

1. `DescriptionKey` + `DescriptionProvider` with matching culture → localized string
2. `DescriptionKey` + `DescriptionProvider` with no culture match → first available translation
3. `DescriptionKey` but no provider, or key not found → `Description` property
4. No `DescriptionKey` set → `Description` property

## Workflow-Level Provider

Set the provider on individual rules, or share one across all rules in a workflow:

```csharp
var provider = new JsonDescriptionProvider("translations.json");

foreach (var rule in workflow.Rules)
{
    rule.DescriptionProvider = provider;
}

workflow.Compile(parameters);
```

---

## Related

- [Rule](rule.md) — `DescriptionKey` and `DescriptionProvider` properties
- [Workflow](workflow.md) — Container for localized rules
- [Rule Metrics](rule-metrics.md) — Track localization coverage


## Thread Safety

`DescriptionProvider` is set before compilation and never modified after. The provider implementation should be thread-safe if you share it across rules.

## Without Localization

If you don't set `DescriptionKey` or `DescriptionProvider`, everything works exactly as before. `GetLocalizedDescription()` returns `Description`. Zero overhead if unused.
