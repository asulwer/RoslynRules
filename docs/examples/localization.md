---
layout: default
title: Localization (i18n)
parent: Examples
nav_order: 7
---

[← Back to Examples Index](index.md)

# Localization (i18n)

Separate rule descriptions from your code so they can be translated without redeploying.

## JSON-Based Translations

**translations.json:**
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

**Provider:**
```csharp
public class JsonDescriptionProvider : IRuleDescriptionProvider
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations;

    public JsonDescriptionProvider(string path)
    {
        var json = File.ReadAllText(path);
        _translations = JsonSerializer.Deserialize<...>(json)!;
    }

    public string? GetDescription(string key, string? culture = null)
    {
        if (!_translations.TryGetValue(key, out var map))
            return null;

        if (culture != null && map.TryGetValue(culture, out var localized))
            return localized;

        return map.Values.FirstOrDefault();
    }
}
```

**Usage:**
```csharp
var provider = new JsonDescriptionProvider("translations.json");

var rule = new Rule
{
    Description = "Adult check",           // Fallback
    DescriptionKey = "rule.adultCheck",    // Lookup key
    DescriptionProvider = provider,
    Expression = "customer.Age >= 18"
};

// Get localized text
var text = rule.GetLocalizedDescription("fr-FR");
// "Vérification adulte"

// Execute — result uses localized description
var result = rule.Execute(parameters);
Console.WriteLine(result.RuleDescription);
// "Vérification adulte"
```

## Resx-Based Translations

```csharp
public class ResxDescriptionProvider : IRuleDescriptionProvider
{
    private readonly ResourceManager _resources;

    public ResxDescriptionProvider(ResourceManager resources)
    {
        _resources = resources;
    }

    public string? GetDescription(string key, string? culture = null)
    {
        var ci = culture != null ? new CultureInfo(culture) : CultureInfo.CurrentUICulture;
        return _resources.GetString(key, ci);
    }
}
```

## Workflow-Wide Provider

Set once, apply to all rules:

```csharp
var provider = new JsonDescriptionProvider("translations.json");

foreach (var rule in workflow.Rules)
{
    rule.DescriptionProvider = provider;
}

workflow.Compile(parameters);
```

## Fallback Chain

`GetLocalizedDescription()` tries in order:

1. `DescriptionKey` + `DescriptionProvider` with exact culture match
2. `DescriptionKey` + `DescriptionProvider` — first available translation
3. `DescriptionKey` set but no provider/key not found → `Description`
4. No `DescriptionKey` → `Description`

## Without Localization

If you don't use `DescriptionKey`, everything works exactly as before. `GetLocalizedDescription()` returns `Description`. Zero overhead if unused.
