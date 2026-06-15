using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class RuleChainingDemo
{
    public static Task Run()
    {
        var discountRule = new Rule
        {
            Description = "Apply VIP discount",
            Expression = "customer.IsVip == true",
            Action = "customer.Name = \"[VIP] \" + customer.Name"
        };

        var vip = DemoRunner.Customers.First(c => c.IsVip);
        var regular = DemoRunner.Customers.First(c => !c.IsVip);

        var compileParam = new RuleParameter("customer", typeof(Customer));
        discountRule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var param = new RuleParameter("customer", typeof(Customer), vip);
        var result = discountRule.Execute(param);
        DemoRunner.PrintResult(result, $"VIP discount applied to {vip.Name}");

        param = new RuleParameter("customer", typeof(Customer), regular);
        result = discountRule.Execute(param);
        DemoRunner.PrintResult(result, $"Non-VIP skipped for {regular.Name}");

        return Task.CompletedTask;
    }
}
