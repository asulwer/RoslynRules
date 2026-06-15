using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace RoslynRules.Xml;

/// <summary>
/// XML Schema (XSD) validator for RoslynRules workflow and rule files.
/// Validates XML structure before deserialization to catch malformed files early.
/// Uses System.Xml.Schema for standards-compliant XSD validation.
/// </summary>
public static class XmlSchemaValidator
{
    private static readonly XmlSchemaSet _workflowSchemaSet;
    private static readonly XmlSchemaSet _ruleSchemaSet;

    static XmlSchemaValidator()
    {
        _workflowSchemaSet = new XmlSchemaSet();
        _workflowSchemaSet.Add("", new XmlTextReader(new StringReader(WorkflowXsd)));
        _workflowSchemaSet.Compile();

        _ruleSchemaSet = new XmlSchemaSet();
        _ruleSchemaSet.Add("", new XmlTextReader(new StringReader(RuleXsd)));
        _ruleSchemaSet.Compile();
    }

    /// <summary>
    /// Validates a workflow XML string against the RoslynRules workflow schema.
    /// Returns a list of validation errors — empty means valid.
    /// </summary>
    public static IReadOnlyList<string> ValidateWorkflow(string xml)
    {
        var errors = new List<string>();
        ValidateAgainstSchema(xml, _workflowSchemaSet, errors);
        return errors;
    }

    /// <summary>
    /// Validates a rule XML string against the RoslynRules rule schema.
    /// Returns a list of validation errors — empty means valid.
    /// </summary>
    public static IReadOnlyList<string> ValidateRule(string xml)
    {
        var errors = new List<string>();
        ValidateAgainstSchema(xml, _ruleSchemaSet, errors);
        return errors;
    }

    /// <summary>
    /// Validates an XML file against the workflow schema.
    /// </summary>
    public static IReadOnlyList<string> ValidateWorkflowFile(string filePath)
        => ValidateWorkflow(File.ReadAllText(filePath));

    /// <summary>
    /// Validates an XML file against the rule schema.
    /// </summary>
    public static IReadOnlyList<string> ValidateRuleFile(string filePath)
        => ValidateRule(File.ReadAllText(filePath));

    // ==================== INTERNAL ====================

    private static void ValidateAgainstSchema(string xml, XmlSchemaSet schemaSet, List<string> errors)
    {
        var settings = new XmlReaderSettings
        {
            Schemas = schemaSet,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings
        };

        settings.ValidationEventHandler += (sender, e) =>
        {
            var severity = e.Severity == XmlSeverityType.Error ? "Error" : "Warning";
            errors.Add($"[{severity}] Line {e.Exception.LineNumber}, Position {e.Exception.LinePosition}: {e.Message}");
        };

        try
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, settings);
            while (xmlReader.Read()) { } // Consume entire document to trigger validation
        }
        catch (XmlException ex)
        {
            errors.Add($"[Error] XML parse error at line {ex.LineNumber}, position {ex.LinePosition}: {ex.Message}");
        }
    }

    // ==================== EMBEDDED XSD SCHEMAS ====================

    private static string BuildWorkflowXsd() => string.Join("\n",
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">",
        "",
        "  <xs:element name=\"Workflow\" type=\"WorkflowType\" />",
        "",
        "  <xs:complexType name=\"WorkflowType\">",
        "    <xs:sequence>",
        "      <xs:element name=\"Description\" type=\"xs:string\" />",
        "      <xs:element name=\"ModifiedBy\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"Rules\" type=\"RulesType\" />",
        "    </xs:sequence>",
        "    <xs:attribute name=\"Id\" type=\"GuidType\" use=\"required\" />",
        "    <xs:attribute name=\"Version\" type=\"SemVerType\" use=\"required\" />",
        "    <xs:attribute name=\"CreatedAt\" type=\"xs:dateTime\" use=\"required\" />",
        "    <xs:attribute name=\"ModifiedAt\" type=\"xs:dateTime\" use=\"required\" />",
        "    <xs:attribute name=\"IsActive\" type=\"xs:boolean\" use=\"required\" />",
        "  </xs:complexType>",
        "",
        "  <xs:complexType name=\"RulesType\">",
        "    <xs:sequence>",
        "      <xs:element name=\"Rule\" type=\"RuleType\" minOccurs=\"1\" maxOccurs=\"unbounded\" />",
        "    </xs:sequence>",
        "  </xs:complexType>",
        "",
        "  <xs:complexType name=\"RuleType\">",
        "    <xs:sequence>",
        "      <xs:element name=\"Description\" type=\"xs:string\" />",
        "      <xs:element name=\"DescriptionKey\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"Expression\" type=\"xs:string\" />",
        "      <xs:element name=\"Action\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"ModifiedBy\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"Timeout\" type=\"xs:double\" minOccurs=\"0\" />",
        "      <xs:element name=\"CacheDuration\" type=\"xs:double\" minOccurs=\"0\" />",
        "      <xs:element name=\"WorkflowId\" type=\"GuidType\" minOccurs=\"0\" />",
        "      <xs:element name=\"ParentRuleId\" type=\"GuidType\" minOccurs=\"0\" />",
        "      <xs:element name=\"DependsOnRuleId\" type=\"GuidType\" minOccurs=\"0\" />",
        "      <xs:element name=\"ChildRules\" type=\"RulesType\" minOccurs=\"0\" />",
        "    </xs:sequence>",
        "    <xs:attribute name=\"Id\" type=\"GuidType\" use=\"required\" />",
        "    <xs:attribute name=\"Version\" type=\"SemVerType\" use=\"required\" />",
        "    <xs:attribute name=\"CreatedAt\" type=\"xs:dateTime\" use=\"required\" />",
        "    <xs:attribute name=\"ModifiedAt\" type=\"xs:dateTime\" use=\"required\" />",
        "    <xs:attribute name=\"IsActive\" type=\"xs:boolean\" use=\"required\" />",
        "    <xs:attribute name=\"Priority\" type=\"xs:int\" use=\"required\" />",
        "  </xs:complexType>",
        "",
        "  <xs:simpleType name=\"GuidType\">",
        "    <xs:restriction base=\"xs:string\">",
        "      <xs:pattern value=\"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\" />",
        "    </xs:restriction>",
        "  </xs:simpleType>",
        "",
        "  <xs:simpleType name=\"SemVerType\">",
        "    <xs:restriction base=\"xs:string\">",
        "      <xs:pattern value=\"[0-9]+\\.[0-9]+\\.[0-9]+(-[A-Za-z0-9.]+)?(\\+[A-Za-z0-9.]+)?\" />",
        "    </xs:restriction>",
        "  </xs:simpleType>",
        "",
        "</xs:schema>"
    );

    private static string BuildRuleXsd() => string.Join("\n",
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">",
        "",
        "  <xs:element name=\"Rule\" type=\"RuleType\" />",
        "",
        "  <xs:complexType name=\"RuleType\">",
        "    <xs:sequence>",
        "      <xs:element name=\"Description\" type=\"xs:string\" />",
        "      <xs:element name=\"DescriptionKey\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"Expression\" type=\"xs:string\" />",
        "      <xs:element name=\"Action\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"ModifiedBy\" type=\"xs:string\" minOccurs=\"0\" />",
        "      <xs:element name=\"Timeout\" type=\"xs:double\" minOccurs=\"0\" />",
        "      <xs:element name=\"CacheDuration\" type=\"xs:double\" minOccurs=\"0\" />",
        "      <xs:element name=\"WorkflowId\" type=\"GuidType\" minOccurs=\"0\" />",
        "      <xs:element name=\"ParentRuleId\" type=\"GuidType\" minOccurs=\"0\" />",
        "      <xs:element name=\"DependsOnRuleId\" type=\"GuidType\" minOccurs=\"0\" />",
        "      <xs:element name=\"ChildRules\" type=\"RulesType\" minOccurs=\"0\" />",
        "    </xs:sequence>",
        "    <xs:attribute name=\"Id\" type=\"GuidType\" use=\"required\" />",
        "    <xs:attribute name=\"Version\" type=\"SemVerType\" use=\"required\" />",
        "    <xs:attribute name=\"CreatedAt\" type=\"xs:dateTime\" use=\"required\" />",
        "    <xs:attribute name=\"ModifiedAt\" type=\"xs:dateTime\" use=\"required\" />",
        "    <xs:attribute name=\"IsActive\" type=\"xs:boolean\" use=\"required\" />",
        "    <xs:attribute name=\"Priority\" type=\"xs:int\" use=\"required\" />",
        "  </xs:complexType>",
        "",
        "  <xs:complexType name=\"RulesType\">",
        "    <xs:sequence>",
        "      <xs:element name=\"Rule\" type=\"RuleType\" minOccurs=\"1\" maxOccurs=\"unbounded\" />",
        "    </xs:sequence>",
        "  </xs:complexType>",
        "",
        "  <xs:simpleType name=\"GuidType\">",
        "    <xs:restriction base=\"xs:string\">",
        "      <xs:pattern value=\"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\" />",
        "    </xs:restriction>",
        "  </xs:simpleType>",
        "",
        "  <xs:simpleType name=\"SemVerType\">",
        "    <xs:restriction base=\"xs:string\">",
        "      <xs:pattern value=\"[0-9]+\\.[0-9]+\\.[0-9]+(-[A-Za-z0-9.]+)?(\\+[A-Za-z0-9.]+)?\" />",
        "    </xs:restriction>",
        "  </xs:simpleType>",
        "",
        "</xs:schema>"
    );

    /// <summary>
    /// XSD schema for Workflow XML files.
    /// </summary>
    public static string WorkflowXsd => BuildWorkflowXsd();

    /// <summary>
    /// XSD schema for standalone Rule XML files.
    /// </summary>
    public static string RuleXsd => BuildRuleXsd();
}
