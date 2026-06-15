using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RoslynRules.Models
{
    /// <summary>
    /// Generates dependency graph visualizations from workflows and rules.
    /// Supports Graphviz DOT format and Mermaid diagram syntax.
    /// </summary>
    public static class RuleGraphVisualizer
    {
        /// <summary>
        /// Generates a Graphviz DOT representation of a workflow's rule dependencies.
        /// </summary>
        /// <param name="workflow">The workflow to visualize.</param>
        /// <param name="includeInactive">If true, includes inactive rules in the graph (shown dashed).</param>
        /// <returns>Graphviz DOT string.</returns>
        public static string ToDot(Workflow workflow, bool includeInactive = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine("digraph Rules {");
            sb.AppendLine("    rankdir=TB;");
            sb.AppendLine($"    label=\"{EscapeDotLabel(workflow.Description)}\";");
            sb.AppendLine("    labelloc=t;");
            sb.AppendLine("    node [shape=box, style=rounded];");
            sb.AppendLine();

            var allRules = FlattenRules(workflow.Rules, includeInactive).ToList();
            var ruleMap = allRules.ToDictionary(r => r.Id);

            // Define nodes
            foreach (var rule in allRules)
            {
                var label = string.IsNullOrEmpty(rule.Description) ? rule.Id.ToString("N")[..8] : EscapeDotLabel(rule.Description);
                var style = rule.IsActive ? "filled" : "dashed,filled";
                var color = rule.IsActive ? "lightblue" : "lightgrey";
                var shape = rule.ChildRules.Any() ? "box3d" : "box";

                sb.AppendLine($"    \"{rule.Id:N}\" [label=\"{label}\", style=\"{style}\", fillcolor=\"{color}\", shape=\"{shape}\"];");
            }

            sb.AppendLine();

            // Define edges
            foreach (var rule in allRules)
            {
                // Dependency edges (DependsOnRuleId)
                if (rule.DependsOnRuleId.HasValue && ruleMap.ContainsKey(rule.DependsOnRuleId.Value))
                {
                    sb.AppendLine($"    \"{rule.DependsOnRuleId.Value:N}\" -> \"{rule.Id:N}\" [color=\"red\", style=\"dashed\", label=\"depends on\"];");
                }

                // Parent-child edges
                foreach (var child in rule.ChildRules.Where(r => includeInactive || r.IsActive))
                {
                    sb.AppendLine($"    \"{rule.Id:N}\" -> \"{child.Id:N}\" [color=\"blue\", label=\"child\"];");
                }
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// Generates a Mermaid diagram representation of a workflow's rule dependencies.
        /// Renders natively in GitHub/GitLab markdown.
        /// </summary>
        /// <param name="workflow">The workflow to visualize.</param>
        /// <param name="includeInactive">If true, includes inactive rules in the graph (shown with different styling).</param>
        /// <returns>Mermaid diagram string.</returns>
        public static string ToMermaid(Workflow workflow, bool includeInactive = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine("graph TD");

            var allRules = FlattenRules(workflow.Rules, includeInactive).ToList();
            var ruleMap = allRules.ToDictionary(r => r.Id);

            foreach (var rule in allRules)
            {
                var nodeId = $"R{rule.Id:N}";
                var label = string.IsNullOrEmpty(rule.Description) ? rule.Id.ToString("N")[..8] : EscapeMermaidLabel(rule.Description);
                var style = rule.IsActive ? "" : ":::inactive";
                var shape = rule.ChildRules.Any() ? "{{" + label + "}}" : "[" + label + "]";

                sb.AppendLine($"    {nodeId}{shape}{style}");
            }

            // Define class for inactive rules
            sb.AppendLine("    classDef inactive fill:#ccc,stroke:#666,stroke-dasharray: 5 5");
            sb.AppendLine();

            foreach (var rule in allRules)
            {
                var nodeId = $"R{rule.Id:N}";

                // Dependency edges
                if (rule.DependsOnRuleId.HasValue && ruleMap.ContainsKey(rule.DependsOnRuleId.Value))
                {
                    var depId = $"R{rule.DependsOnRuleId.Value:N}";
                    sb.AppendLine($"    {depId} -.depends on.-\u003e {nodeId}");
                }

                // Parent-child edges
                foreach (var child in rule.ChildRules.Where(r => includeInactive || r.IsActive))
                {
                    var childId = $"R{child.Id:N}";
                    sb.AppendLine($"    {nodeId} --child--\u003e {childId}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a simplified DOT graph for a single rule tree (the rule and its descendants).
        /// </summary>
        /// <param name="rule">The root rule to visualize.</param>
        /// <param name="includeInactive">If true, includes inactive child rules.</param>
        /// <returns>Graphviz DOT string.</returns>
        public static string ToDot(Rule rule, bool includeInactive = false)
        {
            var workflow = new Workflow { Rules = { rule } };
            return ToDot(workflow, includeInactive);
        }

        /// <summary>
        /// Generates a simplified Mermaid diagram for a single rule tree.
        /// </summary>
        /// <param name="rule">The root rule to visualize.</param>
        /// <param name="includeInactive">If true, includes inactive child rules.</param>
        /// <returns>Mermaid diagram string.</returns>
        public static string ToMermaid(Rule rule, bool includeInactive = false)
        {
            var workflow = new Workflow { Rules = { rule } };
            return ToMermaid(workflow, includeInactive);
        }

        // ==================== HELPERS ====================

        private static IEnumerable<Rule> FlattenRules(IEnumerable<Rule> rules, bool includeInactive)
        {
            foreach (var rule in rules)
            {
                if (includeInactive || rule.IsActive)
                {
                    yield return rule;
                    foreach (var child in FlattenRules(rule.ChildRules, includeInactive))
                    {
                        yield return child;
                    }
                }
                else if (includeInactive)
                {
                    yield return rule;
                }
            }
        }

        private static string EscapeDotLabel(string label)
        {
            return label.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "");
        }

        private static string EscapeMermaidLabel(string label)
        {
            return label.Replace("[", "&#91;")
                        .Replace("]", "&#93;")
                        .Replace("(", "&#40;")
                        .Replace(")", "&#41;")
                        .Replace("{", "&#123;")
                        .Replace("}", "&#125;")
                        .Replace("\n", " ")
                        .Replace("\r", "");
        }
    }
}
