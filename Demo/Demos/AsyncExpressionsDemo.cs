using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class AsyncExpressionsDemo
{
    public static async Task Run()
    {
        var rule = new Rule
        {
            Description = "Async adult check",
            Expression = "await Task.FromResult(customer.Age >= 18)"
        };

        var compileParam = new RuleParameter("customer", typeof(Customer));
        rule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var customer = DemoRunner.Customers.First();
        var param = new RuleParameter("customer", typeof(Customer), customer);
        var result = await rule.ExecuteAsync(param);
        DemoRunner.PrintResult(result, "Async age check");
    }
}
