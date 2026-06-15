using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RoslynRules.Json;

/// <summary>
/// Lightweight JSON schema validator for RoslynRules workflow and rule files.
/// Performs structural validation before deserialization to catch malformed files early.
/// Uses System.Text.Json.Nodes for AOT-safe parsing without reflection.
/// </summary>
public static class JsonSchemaValidator
{
    /// <summary>
    /// Validates a workflow JSON string and returns a list of validation errors.
    /// Empty list means the JSON is structurally valid.
    /// </summary>
    public static IReadOnlyList<string> ValidateWorkflow(string json)
    {
        var errors = new List<string>();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return errors;
        }

        if (root is not JsonObject obj)
        {
            errors.Add("Root element must be a JSON object.");
            return errors;
        }

        // Required fields
        if (!obj.ContainsKey("id"))
            errors.Add("Missing required field: 'id'.");
        else if (!IsValidGuid(obj["id"]))
            errors.Add("Field 'id' must be a valid GUID string.");

        if (!obj.ContainsKey("description"))
            errors.Add("Missing required field: 'description'.");
        else if (obj["description"]?.GetValueKind() != JsonValueKind.String)
            errors.Add("Field 'description' must be a string.");

        if (obj.ContainsKey("version") && !IsValidVersion(obj["version"]))
            errors.Add("Field 'version' must be a valid SemVer string (e.g. '1.0.0').");

        if (!obj.ContainsKey("rules"))
            errors.Add("Missing required field: 'rules'.");
        else
            ValidateRulesArray(obj["rules"], errors, "rules");

        // Optional field type checks
        if (obj.ContainsKey("isActive") && !IsBoolean(obj["isActive"]))
            errors.Add("Field 'isActive' must be a boolean.");

        if (obj.ContainsKey("modifiedBy") && obj["modifiedBy"]?.GetValueKind() != JsonValueKind.String)
            errors.Add("Field 'modifiedBy' must be a string.");

        if (obj.ContainsKey("createdAt") && !IsValidDateTime(obj["createdAt"]))
            errors.Add("Field 'createdAt' must be a valid ISO 8601 date string.");

        if (obj.ContainsKey("modifiedAt") && !IsValidDateTime(obj["modifiedAt"]))
            errors.Add("Field 'modifiedAt' must be a valid ISO 8601 date string.");

        return errors;
    }

    /// <summary>
    /// Validates a rule JSON string and returns a list of validation errors.
    /// Empty list means the JSON is structurally valid.
    /// </summary>
    public static IReadOnlyList<string> ValidateRule(string json)
    {
        var errors = new List<string>();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON: {ex.Message}");
            return errors;
        }

        if (root is not JsonObject obj)
        {
            errors.Add("Root element must be a JSON object.");
            return errors;
        }

        // Required fields
        if (!obj.ContainsKey("id"))
            errors.Add("Missing required field: 'id'.");
        else if (!IsValidGuid(obj["id"]))
            errors.Add("Field 'id' must be a valid GUID string.");

        if (!obj.ContainsKey("description"))
            errors.Add("Missing required field: 'description'.");
        else if (obj["description"]?.GetValueKind() != JsonValueKind.String)
            errors.Add("Field 'description' must be a string.");

        if (!obj.ContainsKey("expression"))
            errors.Add("Missing required field: 'expression'.");
        else if (obj["expression"]?.GetValueKind() != JsonValueKind.String)
            errors.Add("Field 'expression' must be a string.");

        if (obj.ContainsKey("version") && !IsValidVersion(obj["version"]))
            errors.Add("Field 'version' must be a valid SemVer string (e.g. '1.0.0').");

        // Optional field type checks
        if (obj.ContainsKey("isActive") && !IsBoolean(obj["isActive"]))
            errors.Add("Field 'isActive' must be a boolean.");

        if (obj.ContainsKey("priority") && !IsInteger(obj["priority"]))
            errors.Add("Field 'priority' must be an integer.");

        if (obj.ContainsKey("action") && obj["action"]?.GetValueKind() != JsonValueKind.String)
            errors.Add("Field 'action' must be a string.");

        if (obj.ContainsKey("cacheDuration") && !IsValidTimeSpan(obj["cacheDuration"]))
            errors.Add("Field 'cacheDuration' must be a number (seconds) or null.");

        if (obj.ContainsKey("timeout") && !IsValidTimeSpan(obj["timeout"]))
            errors.Add("Field 'timeout' must be a number (seconds) or null.");

        if (obj.ContainsKey("dependsOnRuleId") && !IsValidGuidOrNull(obj["dependsOnRuleId"]))
            errors.Add("Field 'dependsOnRuleId' must be a valid GUID string or null.");

        if (obj.ContainsKey("parentRuleId") && !IsValidGuidOrNull(obj["parentRuleId"]))
            errors.Add("Field 'parentRuleId' must be a valid GUID string or null.");

        if (obj.ContainsKey("workflowId") && !IsValidGuidOrNull(obj["workflowId"]))
            errors.Add("Field 'workflowId' must be a valid GUID string or null.");

        if (obj.ContainsKey("descriptionKey") && obj["descriptionKey"]?.GetValueKind() != JsonValueKind.String)
            errors.Add("Field 'descriptionKey' must be a string.");

        if (obj.ContainsKey("childRules"))
            ValidateRulesArray(obj["childRules"], errors, "childRules");

        return errors;
    }

    /// <summary>
    /// Validates that a JSON node is a non-empty array of rule objects.
    /// </summary>
    private static void ValidateRulesArray(JsonNode? node, List<string> errors, string fieldName)
    {
        if (node is not JsonArray array)
        {
            errors.Add($"Field '{fieldName}' must be an array.");
            return;
        }

        if (array.Count == 0)
        {
            errors.Add($"Field '{fieldName}' must contain at least one rule.");
            return;
        }

        for (int i = 0; i < array.Count; i++)
        {
            var item = array[i];
            if (item is not JsonObject ruleObj)
            {
                errors.Add($"Field '{fieldName}[{i}]' must be a rule object.");
                continue;
            }

            if (!ruleObj.ContainsKey("id"))
                errors.Add($"Field '{fieldName}[{i}].id' is required.");
            else if (!IsValidGuid(ruleObj["id"]))
                errors.Add($"Field '{fieldName}[{i}].id' must be a valid GUID string.");

            if (!ruleObj.ContainsKey("description"))
                errors.Add($"Field '{fieldName}[{i}].description' is required.");
            else if (ruleObj["description"]?.GetValueKind() != JsonValueKind.String)
                errors.Add($"Field '{fieldName}[{i}].description' must be a string.");

            if (!ruleObj.ContainsKey("expression"))
                errors.Add($"Field '{fieldName}[{i}].expression' is required.");
            else if (ruleObj["expression"]?.GetValueKind() != JsonValueKind.String)
                errors.Add($"Field '{fieldName}[{i}].expression' must be a string.");

            if (ruleObj.ContainsKey("version") && !IsValidVersion(ruleObj["version"]))
                errors.Add($"Field '{fieldName}[{i}].version' must be a valid SemVer string.");

            if (ruleObj.ContainsKey("isActive") && !IsBoolean(ruleObj["isActive"]))
                errors.Add($"Field '{fieldName}[{i}].isActive' must be a boolean.");

            if (ruleObj.ContainsKey("priority") && !IsInteger(ruleObj["priority"]))
                errors.Add($"Field '{fieldName}[{i}].priority' must be an integer.");
        }
    }

    // ==================== Type Helpers ====================

    private static bool IsValidGuid(JsonNode? node)
    {
        if (node?.GetValueKind() != JsonValueKind.String)
            return false;

        var value = node.GetValue<string>();
        return Guid.TryParse(value, out _);
    }

    private static bool IsValidGuidOrNull(JsonNode? node)
    {
        if (node is null || node.GetValueKind() == JsonValueKind.Null)
            return true;
        return IsValidGuid(node);
    }

    private static bool IsValidVersion(JsonNode? node)
    {
        if (node?.GetValueKind() != JsonValueKind.String)
            return false;

        var value = node.GetValue<string>();
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Basic SemVer validation: major.minor.patch[-prerelease][+build]
        var parts = value.Split('+');
        var versionPart = parts[0];
        var semverParts = versionPart.Split('-');
        var numberPart = semverParts[0];

        var numbers = numberPart.Split('.');
        if (numbers.Length != 3)
            return false;

        foreach (var n in numbers)
        {
            if (!int.TryParse(n, out var num) || num < 0)
                return false;
        }

        return true;
    }

    private static bool IsBoolean(JsonNode? node)
    {
        if (node is null)
            return false;
        var kind = node.GetValueKind();
        return kind == JsonValueKind.True || kind == JsonValueKind.False;
    }

    private static bool IsInteger(JsonNode? node)
    {
        if (node is null)
            return false;
        var kind = node.GetValueKind();
        return kind == JsonValueKind.Number && node.GetValue<int>() is var _;
    }

    private static bool IsValidDateTime(JsonNode? node)
    {
        if (node?.GetValueKind() != JsonValueKind.String)
            return false;

        var value = node.GetValue<string>();
        return DateTime.TryParse(value, out _);
    }

    private static bool IsValidTimeSpan(JsonNode? node)
    {
        if (node is null || node.GetValueKind() == JsonValueKind.Null)
            return true;

        if (node.GetValueKind() == JsonValueKind.Number)
        {
            return node.GetValue<double>() >= 0;
        }

        return false;
    }
}
