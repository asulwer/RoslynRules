using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class ChildRulesDemo
{
    public static Task Run()
    {
        var parent = new Rule
        {
            Description = "Premium customer validation",
            Expression = "customer.IsVip == true",
            ChildRules = new List<Rule>
            {
                new Rule { Description = "Has orders", Expression = "customer.Orders.Count > 0" },
                new Rule { Description = "Account active", Expression = "customer.IsActive == true" }
            }
        };

        var compileParam = new RuleParameter("customer", typeof(Customer));
        parent.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var premium = DemoRunner.Customers.First(c => c.IsVip && c.Orders.Count > 0 && c.IsActive);
        var param = new RuleParameter("customer", typeof(Customer), premium);
        var result = parent.Execute(param);
        DemoRunner.PrintResult(result, $"Premium check for {premium.Name}");

        if (result.FirstFailure is not null)
            Console.WriteLine($"  First failure: {result.FirstFailure.Value.RuleDescription}");

        return Task.CompletedTask;
    }
}
