using RoslynRules.Models;
using Demo.Models;
using System.Diagnostics;

namespace Demo.Demos;

public static class WorkflowVsRuleBatchDemo
{
    public static Task Run()
    {
        var wf = new Workflow
        {
            Description = "Batch comparison",
            Rules = new List<Rule>
            {
                new Rule { Description = "Rule 1", Expression = "customer.Age >= 18" },
                new Rule { Description = "Rule 2", Expression = "customer.IsActive == true" },
                new Rule { Description = "Rule 3", Expression = "customer.Orders.Count > 0" }
            }
        };

        var customer = DemoRunner.Customers.First(c => c.Age >= 18 && c.IsActive && c.Orders.Count > 0);
        var compileParam = new RuleParameter("customer", typeof(Customer));
        wf.Compile(new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var param = new RuleParameter("customer", typeof(Customer), customer);

        var sw = Stopwatch.StartNew();
        var seq = wf.Execute(param).ToArray();
        sw.Stop();
        Console.WriteLine($"  Sequential: {seq.Length} results in {sw.Elapsed.TotalMilliseconds:F3}ms");

        sw.Restart();
        var par = wf.ExecuteParallel(param);
        sw.Stop();
        Console.WriteLine($"  Parallel:   {par.Length} results in {sw.Elapsed.TotalMilliseconds:F3}ms");

        return Task.CompletedTask;
    }
}
