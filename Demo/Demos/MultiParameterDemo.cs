using RoslynRules.Compiler;
using RoslynRules.Models;

namespace Demo.Demos;

public static class MultiParameterDemo
{
    public static Task Run()
    {
        Console.WriteLine("\n--- Multi-Parameter Rules ---");

        // Example 1: Two parameters
        var rule1 = new Rule
        {
            Description = "Price and quantity check",
            Expression = "price > 0 && quantity > 0",
            IsActive = true
        };

        var compileParams1 = new[]
        {
            new RuleParameter("price", typeof(decimal)),
            new RuleParameter("quantity", typeof(int))
        };
        var compiler = new ExpressionCompiler();
        rule1.Compile(compiler, compileParams1);

        var executeParams1 = new[]
        {
            new RuleParameter("price", typeof(decimal), 9.99m),
            new RuleParameter("quantity", typeof(int), 5)
        };
        var result1 = rule1.Execute(executeParams1);
        Console.WriteLine($"  Price {9.99m:C} x {5} = {result1.Success}");

        // Example 2: Three parameters
        var rule2 = new Rule
        {
            Description = "Age range and name check",
            Expression = "name.Length > 0 && age >= 18 && age <= 65",
            IsActive = true
        };

        var compileParams2 = new[]
        {
            new RuleParameter("name", typeof(string)),
            new RuleParameter("age", typeof(int))
        };
        rule2.Compile(compiler, compileParams2);

        var executeParams2 = new[]
        {
            new RuleParameter("name", typeof(string), "Alice"),
            new RuleParameter("age", typeof(int), 25)
        };
        var result2 = rule2.Execute(executeParams2);
        Console.WriteLine($"  Alice age 25 = {result2.Success}");

        var executeParams2b = new[]
        {
            new RuleParameter("name", typeof(string), "Bob"),
            new RuleParameter("age", typeof(int), 70)
        };
        var result2b = rule2.Execute(executeParams2b);
        Console.WriteLine($"  Bob age 70 = {result2b.Success} (age > 65)");

        // Example 3: Parameter validation - wrong count
        try
        {
            var wrongCount = new[]
            {
                new RuleParameter("price", typeof(decimal), 5.00m)
                // Missing quantity
            };
            rule1.Execute(wrongCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Caught wrong parameter count: {ex.GetType().Name}");
        }

        // Example 4: Parameter validation - wrong name
        try
        {
            var wrongName = new[]
            {
                new RuleParameter("cost", typeof(decimal), 5.00m),
                new RuleParameter("quantity", typeof(int), 1)
            };
            rule1.Execute(wrongName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Caught wrong parameter name: {ex.GetType().Name}");
        }

        return Task.CompletedTask;
    }
}
