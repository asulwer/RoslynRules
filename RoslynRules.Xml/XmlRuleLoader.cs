using RoslynRules.Models;
using System.Globalization;
using System.Xml.Linq;

namespace RoslynRules.Xml;

/// <summary>
/// XML serialization and deserialization for Rules and Workflows.
/// Uses System.Xml.Linq for trim/AOT-safe serialization without reflection.
/// </summary>
public static class XmlRuleLoader
{
    /// <summary>
    /// Serializes a workflow to XML string.
    /// </summary>
    public static string Serialize(Workflow workflow)
    {
        var document = new XDocument(ToXElement(workflow));
        return document.ToString(SaveOptions.None);
    }

    /// <summary>
    /// Serializes a rule to XML string.
    /// </summary>
    public static string Serialize(Rule rule)
    {
        var document = new XDocument(ToXElement(rule));
        return document.ToString(SaveOptions.None);
    }

    /// <summary>
    /// Deserializes a workflow from XML string.
    /// </summary>
    public static Workflow DeserializeWorkflow(string xml)
    {
        var element = XDocument.Parse(xml).Root ?? throw new InvalidOperationException("Invalid XML: missing root element.");
        return ToWorkflow(element);
    }

    /// <summary>
    /// Deserializes a rule from XML string.
    /// </summary>
    public static Rule DeserializeRule(string xml)
    {
        var element = XDocument.Parse(xml).Root ?? throw new InvalidOperationException("Invalid XML: missing root element.");
        return ToRule(element);
    }

    /// <summary>
    /// Loads a workflow from an XML file. Optionally validates against the XML schema before deserialization.
    /// </summary>
    /// <param name="filePath">Path to the XML file.</param>
    /// <param name="validateSchema">When true, validates XML against the workflow schema and throws if invalid.</param>
    public static Workflow LoadWorkflowFromFile(string filePath, bool validateSchema = false)
    {
        var xml = File.ReadAllText(filePath);
        if (validateSchema)
        {
            var errors = XmlSchemaValidator.ValidateWorkflow(xml);
            if (errors.Count > 0)
                throw new InvalidOperationException($"XML schema validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
        return DeserializeWorkflow(xml);
    }

    /// <summary>
    /// Loads a rule from an XML file. Optionally validates against the XML schema before deserialization.
    /// </summary>
    /// <param name="filePath">Path to the XML file.</param>
    /// <param name="validateSchema">When true, validates XML against the rule schema and throws if invalid.</param>
    public static Rule LoadRuleFromFile(string filePath, bool validateSchema = false)
    {
        var xml = File.ReadAllText(filePath);
        if (validateSchema)
        {
            var errors = XmlSchemaValidator.ValidateRule(xml);
            if (errors.Count > 0)
                throw new InvalidOperationException($"XML schema validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
        return DeserializeRule(xml);
    }

    /// <summary>
    /// Saves a workflow to an XML file.
    /// </summary>
    public static void SaveWorkflowToFile(Workflow workflow, string filePath)
        => File.WriteAllText(filePath, Serialize(workflow));

    /// <summary>
    /// Saves a rule to an XML file.
    /// </summary>
    public static void SaveRuleToFile(Rule rule, string filePath)
        => File.WriteAllText(filePath, Serialize(rule));

    // ==================== INTERNAL SERIALIZATION HELPERS ====================

    private static XElement ToXElement(Workflow workflow)
    {
        var element = new XElement("Workflow",
            new XAttribute("Id", workflow.Id),
            new XAttribute("Version", workflow.Version.ToString()),
            new XAttribute("CreatedAt", workflow.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("ModifiedAt", workflow.ModifiedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("IsActive", workflow.IsActive),
            new XElement("Description", workflow.Description),
            ElementOrNull("ModifiedBy", workflow.ModifiedBy)
        );

        var rulesElement = new XElement("Rules");
        foreach (var rule in workflow.Rules)
        {
            rulesElement.Add(ToXElement(rule));
        }
        element.Add(rulesElement);

        return element;
    }

    private static XElement ToXElement(Rule rule)
    {
        var element = new XElement("Rule",
            new XAttribute("Id", rule.Id),
            new XAttribute("Version", rule.Version.ToString()),
            new XAttribute("CreatedAt", rule.CreatedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("ModifiedAt", rule.ModifiedAt.ToString("O", CultureInfo.InvariantCulture)),
            new XAttribute("IsActive", rule.IsActive),
            new XAttribute("Priority", rule.Priority),
            new XElement("Description", rule.Description),
            ElementOrNull("DescriptionKey", rule.DescriptionKey),
            new XElement("Expression", rule.Expression),
            new XElement("Action", rule.Action),
            ElementOrNull("ModifiedBy", rule.ModifiedBy)
        );

        if (rule.Timeout.HasValue)
            element.Add(new XElement("Timeout", rule.Timeout.Value.TotalSeconds));

        if (rule.CacheDuration.HasValue)
            element.Add(new XElement("CacheDuration", rule.CacheDuration.Value.TotalSeconds));

        if (rule.WorkflowId.HasValue)
            element.Add(new XElement("WorkflowId", rule.WorkflowId.Value));

        if (rule.ParentRuleId.HasValue)
            element.Add(new XElement("ParentRuleId", rule.ParentRuleId.Value));

        if (rule.DependsOnRuleId.HasValue)
            element.Add(new XElement("DependsOnRuleId", rule.DependsOnRuleId.Value));

        if (rule.ChildRules.Any())
        {
            var children = new XElement("ChildRules");
            foreach (var child in rule.ChildRules)
            {
                children.Add(ToXElement(child));
            }
            element.Add(children);
        }

        return element;
    }

    private static XElement? ElementOrNull(string name, string? value)
        => string.IsNullOrEmpty(value) ? null : new XElement(name, value);

    // ==================== INTERNAL DESERIALIZATION HELPERS ====================

    private static Workflow ToWorkflow(XElement element)
    {
        var workflow = new Workflow
        {
            Id = ParseGuid(element.Attribute("Id")?.Value),
            Version = ParseVersion(element.Attribute("Version")?.Value),
            CreatedAt = ParseDateTime(element.Attribute("CreatedAt")?.Value),
            ModifiedAt = ParseDateTime(element.Attribute("ModifiedAt")?.Value),
            IsActive = ParseBool(element.Attribute("IsActive")?.Value, true),
            Description = element.Element("Description")?.Value ?? string.Empty,
            ModifiedBy = element.Element("ModifiedBy")?.Value
        };

        var rulesElement = element.Element("Rules");
        if (rulesElement != null)
        {
            foreach (var ruleElement in rulesElement.Elements("Rule"))
            {
                workflow.Rules.Add(ToRule(ruleElement));
            }
        }

        return workflow;
    }

    private static Rule ToRule(XElement element)
    {
        var rule = new Rule(ParseGuid(element.Attribute("Id")?.Value))
        {
            Version = ParseVersion(element.Attribute("Version")?.Value),
            CreatedAt = ParseDateTime(element.Attribute("CreatedAt")?.Value),
            ModifiedAt = ParseDateTime(element.Attribute("ModifiedAt")?.Value),
            IsActive = ParseBool(element.Attribute("IsActive")?.Value, true),
            Priority = ParseInt(element.Attribute("Priority")?.Value, 0),
            Description = element.Element("Description")?.Value ?? string.Empty,
            DescriptionKey = element.Element("DescriptionKey")?.Value,
            Expression = element.Element("Expression")?.Value ?? string.Empty,
            Action = element.Element("Action")?.Value ?? string.Empty,
            ModifiedBy = element.Element("ModifiedBy")?.Value
        };

        var timeoutElement = element.Element("Timeout");
        if (timeoutElement != null && double.TryParse(timeoutElement.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var timeoutSeconds))
            rule.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var cacheDurationElement = element.Element("CacheDuration");
        if (cacheDurationElement != null && double.TryParse(cacheDurationElement.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var cacheSeconds))
            rule.CacheDuration = TimeSpan.FromSeconds(cacheSeconds);

        var workflowIdElement = element.Element("WorkflowId");
        if (workflowIdElement != null && Guid.TryParse(workflowIdElement.Value, out var workflowId))
            rule.WorkflowId = workflowId;

        var parentRuleIdElement = element.Element("ParentRuleId");
        if (parentRuleIdElement != null && Guid.TryParse(parentRuleIdElement.Value, out var parentRuleId))
            rule.ParentRuleId = parentRuleId;

        var dependsOnRuleIdElement = element.Element("DependsOnRuleId");
        if (dependsOnRuleIdElement != null && Guid.TryParse(dependsOnRuleIdElement.Value, out var dependsOnRuleId))
            rule.DependsOnRuleId = dependsOnRuleId;

        var childRulesElement = element.Element("ChildRules");
        if (childRulesElement != null)
        {
            foreach (var childElement in childRulesElement.Elements("Rule"))
            {
                rule.ChildRules.Add(ToRule(childElement));
            }
        }

        return rule;
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
