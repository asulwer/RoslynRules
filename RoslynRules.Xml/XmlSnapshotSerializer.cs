using RoslynRules.Models;
using RoslynRules.Snapshots;
using System.Globalization;
using System.Xml.Linq;

namespace RoslynRules.Xml;

/// <summary>
/// XML serializer for workflow and rule snapshots.
/// Uses System.Xml.Linq for AOT/trimming compatibility.
/// 
/// JIT: Can serialize (create snapshots) and deserialize.
/// AOT: Can only deserialize pre-existing snapshots.
/// </summary>
public sealed class XmlSnapshotSerializer : ISnapshotSerializer
{
    /// <inheritdoc />
    public string Serialize(WorkflowSnapshot snapshot)
    {
        var document = new XDocument(ToXElement(snapshot));
        return document.ToString(SaveOptions.None);
    }

    /// <inheritdoc />
    public WorkflowSnapshot DeserializeWorkflow(string data)
    {
        var element = XDocument.Parse(data).Root ?? throw new InvalidOperationException("Invalid XML: missing root element.");
        return ToWorkflowSnapshot(element);
    }

    /// <inheritdoc />
    public string Serialize(RuleSnapshot snapshot)
    {
        var document = new XDocument(ToXElement(snapshot));
        return document.ToString(SaveOptions.None);
    }

    /// <inheritdoc />
    public RuleSnapshot DeserializeRule(string data)
    {
        var element = XDocument.Parse(data).Root ?? throw new InvalidOperationException("Invalid XML: missing root element.");
        return ToRuleSnapshot(element);
    }

    // ==================== SERIALIZATION ====================

    private static XElement ToXElement(WorkflowSnapshot snapshot)
    {
        var element = new XElement("WorkflowSnapshot",
            new XAttribute("Id", snapshot.Id),
            new XAttribute("Version", snapshot.Version.ToString()),
            new XAttribute("CreatedAt", snapshot.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("ModifiedAt", snapshot.ModifiedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("IsActive", snapshot.IsActive),
            ElementOrNull("Description", snapshot.Description),
            ElementOrNull("ModifiedBy", snapshot.ModifiedBy)
        );

        var rulesElement = new XElement("Rules");
        foreach (var rule in snapshot.Rules)
        {
            rulesElement.Add(ToXElement(rule));
        }
        element.Add(rulesElement);

        return element;
    }

    private static XElement ToXElement(RuleSnapshot snapshot)
    {
        var element = new XElement("RuleSnapshot",
            new XAttribute("Id", snapshot.Id),
            new XAttribute("Version", snapshot.Version.ToString()),
            new XAttribute("CreatedAt", snapshot.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("ModifiedAt", snapshot.ModifiedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("IsActive", snapshot.IsActive),
            new XAttribute("Priority", snapshot.Priority),
            ElementOrNull("Description", snapshot.Description),
            ElementOrNull("DescriptionKey", snapshot.DescriptionKey),
            ElementOrNull("Expression", snapshot.Expression),
            ElementOrNull("Action", snapshot.Action),
            ElementOrNull("ModifiedBy", snapshot.ModifiedBy)
        );

        if (snapshot.Timeout.HasValue)
            element.Add(new XElement("Timeout", snapshot.Timeout.Value.TotalSeconds));

        if (snapshot.CacheDuration.HasValue)
            element.Add(new XElement("CacheDuration", snapshot.CacheDuration.Value.TotalSeconds));

        if (snapshot.WorkflowId.HasValue)
            element.Add(new XElement("WorkflowId", snapshot.WorkflowId.Value));

        if (snapshot.ParentRuleId.HasValue)
            element.Add(new XElement("ParentRuleId", snapshot.ParentRuleId.Value));

        if (snapshot.DependsOnRuleId.HasValue)
            element.Add(new XElement("DependsOnRuleId", snapshot.DependsOnRuleId.Value));

        if (snapshot.ChildRules.Any())
        {
            var children = new XElement("ChildRules");
            foreach (var child in snapshot.ChildRules)
            {
                children.Add(ToXElement(child));
            }
            element.Add(children);
        }

        return element;
    }

    private static XElement? ElementOrNull(string name, string? value)
        => string.IsNullOrEmpty(value) ? null : new XElement(name, value);

    // ==================== DESERIALIZATION ====================

    private static WorkflowSnapshot ToWorkflowSnapshot(XElement element)
    {
        var rulesElement = element.Element("Rules");
        var rules = rulesElement != null
            ? rulesElement.Elements("RuleSnapshot").Select(ToRuleSnapshot).ToList()
            : new List<RuleSnapshot>();

        return new WorkflowSnapshot
        {
            Id = ParseGuid(element.Attribute("Id")?.Value),
            Version = ParseVersion(element.Attribute("Version")?.Value),
            CreatedAt = ParseDateTime(element.Attribute("CreatedAt")?.Value),
            ModifiedAt = ParseDateTime(element.Attribute("ModifiedAt")?.Value),
            IsActive = ParseBool(element.Attribute("IsActive")?.Value, true),
            Description = element.Element("Description")?.Value ?? string.Empty,
            ModifiedBy = element.Element("ModifiedBy")?.Value,
            Rules = rules
        };
    }

    private static RuleSnapshot ToRuleSnapshot(XElement element)
    {
        TimeSpan? timeout = null;
        var timeoutElement = element.Element("Timeout");
        if (timeoutElement != null && double.TryParse(timeoutElement.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutSeconds))
            timeout = TimeSpan.FromSeconds(timeoutSeconds);

        TimeSpan? cacheDuration = null;
        var cacheDurationElement = element.Element("CacheDuration");
        if (cacheDurationElement != null && double.TryParse(cacheDurationElement.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cacheSeconds))
            cacheDuration = TimeSpan.FromSeconds(cacheSeconds);

        Guid? workflowId = null;
        var workflowIdElement = element.Element("WorkflowId");
        if (workflowIdElement != null && Guid.TryParse(workflowIdElement.Value, out var wfId))
            workflowId = wfId;

        Guid? parentRuleId = null;
        var parentRuleIdElement = element.Element("ParentRuleId");
        if (parentRuleIdElement != null && Guid.TryParse(parentRuleIdElement.Value, out var prId))
            parentRuleId = prId;

        Guid? dependsOnRuleId = null;
        var dependsOnRuleIdElement = element.Element("DependsOnRuleId");
        if (dependsOnRuleIdElement != null && Guid.TryParse(dependsOnRuleIdElement.Value, out var drId))
            dependsOnRuleId = drId;

        var childRulesElement = element.Element("ChildRules");
        var childRules = childRulesElement != null
            ? childRulesElement.Elements("RuleSnapshot").Select(ToRuleSnapshot).ToList()
            : new List<RuleSnapshot>();

        return new RuleSnapshot
        {
            Id = ParseGuid(element.Attribute("Id")?.Value),
            Version = ParseVersion(element.Attribute("Version")?.Value),
            CreatedAt = ParseDateTime(element.Attribute("CreatedAt")?.Value),
            ModifiedAt = ParseDateTime(element.Attribute("ModifiedAt")?.Value),
            IsActive = ParseBool(element.Attribute("IsActive")?.Value, true),
            Priority = ParseInt(element.Attribute("Priority")?.Value, 0),
            Description = element.Element("Description")?.Value ?? string.Empty,
            DescriptionKey = element.Element("DescriptionKey")?.Value,
            Expression = element.Element("Expression")?.Value ?? string.Empty,
            Action = element.Element("Action")?.Value ?? string.Empty,
            ModifiedBy = element.Element("ModifiedBy")?.Value,
            Timeout = timeout,
            CacheDuration = cacheDuration,
            WorkflowId = workflowId,
            ParentRuleId = parentRuleId,
            DependsOnRuleId = dependsOnRuleId,
            ChildRules = childRules
        };
    }

    // ==================== PARSING HELPERS ====================

    private static Guid ParseGuid(string? value)
        => Guid.TryParse(value, out var result) ? result : Guid.NewGuid();

    private static RuleVersion ParseVersion(string? value)
        => string.IsNullOrEmpty(value) ? new RuleVersion(1, 0, 0) : RuleVersion.Parse(value);

    private static DateTime ParseDateTime(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)
            ? result
            : DateTime.UtcNow;

    private static bool ParseBool(string? value, bool defaultValue)
        => bool.TryParse(value, out var result) ? result : defaultValue;

    private static int ParseInt(string? value, int defaultValue)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
}
