using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class PriorityOrderingDemo
{
    public static Task Run()
    {
        var wf = new Workflow
        {
            Description = "Priority test",
            Rules = new List<Rule>
            {
                new Rule { Description = "Last", Expression = "true", Priority = 10 },
                new Rule { Description = "First", Expression = "true", Priority = 100 },
                new Rule { Description = "Middle", Expression = "true", Priority = 50 }
            }
        };

        var compileParam = new RuleParameter("customer", typeof(Customer));
        wf.Compile(new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var param = new RuleParameter("customer", typeof(Customer), DemoRunner.Customers.First());
        var results = wf.Execute(param).ToArray();

        foreach (var r in results)
            Console.WriteLine($"  [{r.Success}] {r.RuleDescription}");

        return Task.CompletedTask;
    }
}
