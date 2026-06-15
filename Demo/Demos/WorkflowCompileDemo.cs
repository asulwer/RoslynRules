using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class WorkflowCompileDemo
{
    public static Task Run()
    {
        var wf = new Workflow
        {
            Description = "Pre-compiled workflow",
            Rules = new List<Rule>
            {
                new Rule { Description = "Adult check", Expression = "customer.Age >= 18" },
                new Rule { Description = "Active check", Expression = "customer.IsActive == true" },
                new Rule { Description = "Has orders", Expression = "customer.Orders.Count > 0" }
            }
        };

        // Compile once with type-only parameters (values can be null/default)
        var compileParams = new[]
        {
            new RuleParameter("customer", typeof(Customer))
        };
        wf.Compile(compileParams, null, DemoRunner.ReferenceProvider);

        // Execute multiple times with different instances
        var adult = DemoRunner.Customers.First(c => c.Age >= 18 && c.IsActive && c.Orders.Count > 0);
        var minor = DemoRunner.Customers.MinBy(c => c.Age) ?? adult; // Use youngest customer as "minor"

        var executeParams1 = new[] { new RuleParameter("customer", typeof(Customer), adult) };
        var results1 = wf.Execute(executeParams1).ToArray();
        Console.WriteLine($"  Adult: {results1.Length} rules, all passed: {results1.All(r => r.Success)}");

        var executeParams2 = new[] { new RuleParameter("customer", typeof(Customer), minor) };
        var results2 = wf.Execute(executeParams2).ToArray();
        var firstFailure = results2.FirstOrDefault(r => !r.Success);
        Console.WriteLine($"  Minor: {results2.Length} rules, first failure: {firstFailure.RuleDescription}");

        // Parameter validation: wrong name throws RuleValidationException
        try
        {
            var badParams = new[] { new RuleParameter("cust", typeof(Customer), adult) };
            wf.Execute(badParams);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Caught name mismatch: {ex.GetType().Name}");
        }

        return Task.CompletedTask;
    }
}
