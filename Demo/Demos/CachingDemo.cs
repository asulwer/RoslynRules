using RoslynRules.Models;
using Demo.Models;
using System.Diagnostics;

namespace Demo.Demos;

public static class CachingDemo
{
    public static Task Run()
    {
        var rule = new Rule
        {
            Description = "Cached check",
            Expression = "customer.IsActive == true",
            CacheDuration = TimeSpan.FromSeconds(5)
        };

        var compileParam = new RuleParameter("customer", typeof(Customer));
        rule.Compile(DemoRunner.Compiler, new[] { compileParam }, null, DemoRunner.ReferenceProvider);

        var customer = DemoRunner.Customers.First(c => c.IsActive);
        var param = new RuleParameter("customer", typeof(Customer), customer);

        var sw = Stopwatch.StartNew();
        var r1 = rule.Execute(param);
        var first = sw.Elapsed.TotalMilliseconds;

        sw.Restart();
        var r2 = rule.Execute(param);
        var cached = sw.Elapsed.TotalMilliseconds;

        DemoRunner.PrintResult(r1, $"First call ({first:F3}ms)");
        DemoRunner.PrintResult(r2, $"Cached call ({cached:F3}ms)");

        return Task.CompletedTask;
    }
}
