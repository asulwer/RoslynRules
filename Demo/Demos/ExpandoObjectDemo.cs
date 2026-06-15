using RoslynRules.Models;
using System.Collections.Generic;

namespace Demo.Demos;

public static class ExpandoObjectDemo
{
    public static Task Run()
    {
        // Use Dictionary<string, object> instead of ExpandoObject for compile-time compatibility
        var data = new Dictionary<string, object>
        {
            ["name"] = "Dynamic User",
            ["score"] = 95
        };

        var rule = new Rule
        {
            Description = "High score",
            Expression = "data.ContainsKey(\"score\") && (int)data[\"score\"] >= 90"
        };

        var compileParam = new RuleParameter("data", typeof(Dictionary<string, object>));
        rule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var param = new RuleParameter("data", typeof(Dictionary<string, object>), data);
        var result = rule.Execute(param);
        DemoRunner.PrintResult(result, "Dynamic score check");

        return Task.CompletedTask;
    }
}
