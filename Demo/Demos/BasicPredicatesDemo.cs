using RoslynRules.Models;
using Demo.Models;

namespace Demo.Demos;

public static class BasicPredicatesDemo
{
    public static Task Run()
    {
        var rule = new Rule
        {
            Description = "Adult customer",
            Expression = "customer.Age >= 18"
        };

        var adult = DemoRunner.Customers.First(c => c.Age >= 18);
        var minor = DemoRunner.Customers.MinBy(c => c.Age) ?? adult; // Use youngest as "minor" if no actual minors

        // Compile once, execute multiple times
        var compileParam = new RuleParameter("customer", typeof(Customer));
        rule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var param = new RuleParameter("customer", typeof(Customer), adult);
        var result = rule.Execute(param);
        DemoRunner.PrintResult(result, "Adult customer check");

        param = new RuleParameter("customer", typeof(Customer), minor);
        result = rule.Execute(param);
        DemoRunner.PrintResult(result, "Minor customer check");

        // Regex email validation
        var emailRule = new Rule
        {
            Description = "Valid email",
            Expression = "System.Text.RegularExpressions.Regex.IsMatch(customer.Email, \"^[^@]+@[^@]+\\\\.[^@]+$\")"
        };
        emailRule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        param = new RuleParameter("customer", typeof(Customer), adult);
        result = emailRule.Execute(param);
        DemoRunner.PrintResult(result, "Email format check");

        return Task.CompletedTask;
    }
}
