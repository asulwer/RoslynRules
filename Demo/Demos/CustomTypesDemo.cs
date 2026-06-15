using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class CustomTypesDemo
{
    public static Task Run()
    {
        var rule = new Rule
        {
            Description = "Has premium orders",
            Expression = "customer.Orders.Any(o => o.Total > 200)"
        };

        var compileParam = new RuleParameter("customer", typeof(Customer));
        rule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var bigSpender = DemoRunner.Customers.First(c => c.Orders.Any(o => o.Total > 200));
        var param = new RuleParameter("customer", typeof(Customer), bigSpender);
        var result = rule.Execute(param);
        DemoRunner.PrintResult(result, $"Big spender check ({bigSpender.Name})");

        return Task.CompletedTask;
    }
}
