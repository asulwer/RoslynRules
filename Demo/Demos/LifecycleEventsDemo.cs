using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class LifecycleEventsDemo
{
    public static Task Run()
    {
        var rule = new Rule
        {
            Description = "Evented rule",
            Expression = "customer.IsActive == true"
        };
        rule.OnRuleExecuted += (sender, args) =>
            Console.WriteLine($"  Event: '{args.Rule.Description}' executed in {args.Elapsed.TotalMilliseconds:F2}ms");

        var compileParam = new RuleParameter("customer", typeof(Customer));
        rule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var customer = DemoRunner.Customers.First(c => c.IsActive);
        var param = new RuleParameter("customer", typeof(Customer), customer);
        var result = rule.Execute(param);
        DemoRunner.PrintResult(result, "Lifecycle event check");

        return Task.CompletedTask;
    }
}
