using RoslynRules.Models;
using RoslynRules.Templates;
using Demo.Models;

namespace Demo.Demos;

public static class TemplateInstantiationDemo
{
    public static Task Run()
    {
        var template = new RuleTemplate
        {
            Description = "Age threshold check",
            Expression = "customer.Age >= {minAge}",
        };
        template.Placeholders.Add("minAge", PlaceholderKind.Value);

        var values = new Dictionary<string, object> { ["minAge"] = 21 };
        var compileParam = new RuleParameter("customer", typeof(Customer));
        var param = new RuleParameter("customer", typeof(Customer), DemoRunner.Customers.First(c => c.Age >= 21));
        var rule = template.Instantiate(values, DemoRunner.Compiler, new[] { param }, new[] { "Demo.Models" }, DemoRunner.ReferenceProvider);
        var result = rule.Execute(param);
        DemoRunner.PrintResult(result, "Template: age >= 21");

        return Task.CompletedTask;
    }
}
