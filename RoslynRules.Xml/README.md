# RoslynRules.Xml

XML serialization support for RoslynRules. Save and load workflows and rules from XML for configuration-driven rule sets.

## Installation

```bash
dotnet add package RoslynRules.Xml
```

## Usage

### XmlRuleLoader — Live Model Serialization

`XmlRuleLoader` serializes and deserializes live `Workflow` and `Rule` instances.
Use this for configuration files where rules are loaded and then compiled at runtime.

#### Serialize

```csharp
using RoslynRules.Xml;
using RoslynRules.Models;

var workflow = new Workflow
{
    Description = "Validation rules",
    Rules = new List<Rule>
    {
        new Rule { Description = "Adult check", Expression = "customer.Age >= 18" }
    }
};

var xml = XmlRuleLoader.Serialize(workflow);
File.WriteAllText("rules.xml", xml);
```

#### Deserialize

```csharp
var workflow = XmlRuleLoader.DeserializeWorkflow(xml);
workflow.Compile(new[] { new RuleParameter("customer", typeof(Customer)) });
var results = workflow.Execute(new[] { new RuleParameter("customer", typeof(Customer), customer) });
```

#### File Helpers

```csharp
XmlRuleLoader.SaveWorkflowToFile(workflow, "rules.xml");
var loaded = XmlRuleLoader.LoadWorkflowFromFile("rules.xml");
```

#### Schema Validation

Validate XML against an embedded XSD schema before deserialization.

```csharp
// Validate during load — throws InvalidOperationException with line/position details
var workflow = XmlRuleLoader.LoadWorkflowFromFile("rules.xml", validateSchema: true);

// Or validate manually
var errors = XmlSchemaValidator.ValidateWorkflow(xml);
if (errors.Count > 0)
{
    Console.WriteLine(string.Join("\n", errors));
}
```

Validation checks: element ordering, required elements/attributes, GUID format, SemVer format (including prerelease and build metadata), boolean/integer/double types, and nested child rule structure.

### XmlSnapshotSerializer — AOT-Safe Snapshot Serialization

`XmlSnapshotSerializer` implements `ISnapshotSerializer` for AOT-safe persistence of compiled rule snapshots.
Unlike `XmlRuleLoader` which works with live models, `XmlSnapshotSerializer` works with immutable `WorkflowSnapshot` and `RuleSnapshot` objects.

#### JIT vs AOT Usage

| Capability | JIT | AOT |
|-----------|-----|-----|
| Create snapshots from compiled rules | Yes | No |
| Save snapshots to XML | Yes | Yes |
| Load snapshots from XML | Yes | Yes |
| Restore workflows from snapshots | Yes | Yes |
| Execute restored workflows | Yes | Yes* |

\*Restored workflows are not compiled; in AOT mode you cannot compile, so snapshots must be pre-compiled in JIT and then loaded in AOT.

#### Serialize a Snapshot (JIT)

```csharp
using RoslynRules.Snapshots;
using RoslynRules.Xml;

// 1. Compile workflow in JIT
var workflow = XmlRuleLoader.LoadWorkflowFromFile("rules.xml");
workflow.Compile(parameters);

// 2. Create snapshot from compiled workflow
var compiled = CompiledWorkflow.Compile(workflow, parameters);
var snapshot = compiled.ToSnapshot();

// 3. Serialize to XML
var serializer = new XmlSnapshotSerializer();
var xml = serializer.Serialize(snapshot);
File.WriteAllText("workflow.snap.xml", xml);
```

#### Deserialize a Snapshot (AOT Safe)

```csharp
// AOT-safe: no compilation, no reflection
var serializer = new XmlSnapshotSerializer();
var snapshot = serializer.DeserializeWorkflow(xml);

// Restore workflow (rules are not compiled)
var workflow = SnapshotManager.RestoreWorkflow(snapshot);
```

#### File Helpers

```csharp
// Save snapshot to file
serializer.SaveWorkflowToFile(snapshot, "workflow.snap.xml");

// Load snapshot from file
var loaded = serializer.LoadWorkflowFromFile("workflow.snap.xml");
```

#### Using SnapshotManager

```csharp
// JIT: compile, snapshot, and save
var compiled = CompiledWorkflow.Compile(workflow, parameters);
var snapshot = SnapshotManager.CreateSnapshot(compiled);
SnapshotManager.SaveSnapshot(snapshot, new XmlSnapshotSerializer(), "rules.snap.xml");

// AOT: load and restore
var loaded = SnapshotManager.LoadSnapshot(new XmlSnapshotSerializer(), "rules.snap.xml");
var restored = SnapshotManager.RestoreWorkflow(loaded);
```

### XML Format

#### Live Model (XmlRuleLoader)

```xml
<Workflow Id="550e8400-e29b-41d4-a716-446655440000" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true">
  <Description>Validation rules</Description>
  <Rules>
    <Rule Id="6ba7b810-9dad-11d1-80b4-00c04fd430c8" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true" Priority="0">
      <Description>Adult check</Description>
      <Expression>customer.Age >= 18</Expression>
    </Rule>
  </Rules>
</Workflow>
```

#### Snapshot (XmlSnapshotSerializer)

```xml
<WorkflowSnapshot Id="550e8400-e29b-41d4-a716-446655440000" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true">
  <Description>Validation rules</Description>
  <Rules>
    <RuleSnapshot Id="6ba7b810-9dad-11d1-80b4-00c04fd430c8" Version="1.0.0" CreatedAt="2024-01-01T00:00:00.0000000Z" ModifiedAt="2024-01-01T00:00:00.0000000Z" IsActive="true" Priority="0">
      <Description>Adult check</Description>
      <Expression>customer.Age >= 18</Expression>
    </RuleSnapshot>
  </Rules>
</WorkflowSnapshot>
```

## AOT Compatibility

This extension uses `System.Xml.Linq` (XDocument) which is compatible with AOT/trimming. No reflection-based serialization is used. `XmlSnapshotSerializer` is fully AOT-safe for deserialization.
