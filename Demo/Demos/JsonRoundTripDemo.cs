using RoslynRules.Json;
using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class JsonRoundTripDemo
{
    public static Task Run()
    {
        var wf = new Workflow
        {
            Description = "JSON test",
            Rules = new List<Rule>
            {
                new Rule { Description = "Active check", Expression = "customer.IsActive == true" }
            }
        };

        var json = JsonRuleLoader.Serialize(wf);
        var restored = JsonRuleLoader.DeserializeWorkflow(json);

        var compileParam = new RuleParameter("customer", typeof(Customer));
        restored.Compile(new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var customer = DemoRunner.Customers.First(c => c.IsActive);
        var param = new RuleParameter("customer", typeof(Customer), customer);
        var result = restored.Execute(param).First();
        DemoRunner.PrintResult(result, "Round-trip workflow");

        return Task.CompletedTask;
    }
}
