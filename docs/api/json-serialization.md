---
layout: default
title: JSON Serialization
parent: API Reference
nav_order: 8
---

[← Back to API Reference](api-reference.md)

# JSON Serialization

Install the `RoslynRules.Json` package:

```bash
dotnet add package RoslynRules.Json
```

Save and load rules/workflows from JSON for configuration-driven setups.

```csharp
using RoslynRules.Json;
```

---

## Methods

### `Serialize(Workflow|Rule, JsonSerializerOptions?)`

Serializes to JSON string.

```csharp
var json = JsonRuleLoader.Serialize(workflow);
File.WriteAllText("rules.json", json);
```

### `DeserializeWorkflow(string)` / `DeserializeRule(string)`

Deserializes from JSON string.

```csharp
var workflow = JsonRuleLoader.DeserializeWorkflow(json);
workflow.Validate();
workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
```

### `LoadWorkflowFromFile(string)` / `LoadRuleFromFile(string)`

Loads from file path.

```csharp
var workflow = JsonRuleLoader.LoadWorkflowFromFile("rules.json");
```

### `SaveWorkflowToFile(Workflow, string)` / `SaveRuleToFile(Rule, string)`

Saves to file path.

```csharp
JsonRuleLoader.SaveWorkflowToFile(workflow, "rules.json");
```

---

## JSON Options

Custom `JsonSerializerOptions` with camelCase naming and indented output.

```csharp

---

## Related

- [Rule](rule.md) — Serialized model
- [Workflow](workflow.md) — Serialized container
- [Rule Templates](rule-templates.md) — Serialize templates before instantiation

var options = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter() }
};

var json = JsonRuleLoader.Serialize(workflow, options);
```

---

## See Also

- [Getting Started](getting-started.md)
- [API Reference: Workflow](workflow.md)
- [API Reference: Rule](rule.md)
- [NuGet Package](https://www.nuget.org/packages/RoslynRules.Json)
