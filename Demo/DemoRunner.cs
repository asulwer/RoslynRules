using Demo.Models;
using System.Text.Json;

namespace Demo;

/// <summary>
/// Shared state and utilities for all demos.
/// </summary>
public static class DemoRunner
{
    public static List<Customer> Customers { get; } = new();
    public static int Section { get; private set; } = 0;

    public static RoslynRules.Compiler.ExpressionCompiler Compiler { get; } = new RoslynRules.Compiler.ExpressionCompiler();
    public static RoslynRules.Compiler.AssemblyReferenceProvider ReferenceProvider { get; }

    static DemoRunner()
    {
        // Add Demo assembly to whitelist so expressions can resolve Customer, Order types
        ReferenceProvider = new RoslynRules.Compiler.AssemblyReferenceProvider(
            RoslynRules.Compiler.AssemblyReferenceProvider.DefaultWhitelist.Concat(new[] { "Demo", "Microsoft.EntityFrameworkCore" }));
    }

    public static void LoadCustomers()
    {
        // Resolve relative to the app's base directory (where Data/Customers.json is copied)
        // rather than the current working directory, so the demo runs from any location
        // (e.g. `dotnet run` from the repo root, not just from the output folder).
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "Customers.json");
        var json = File.ReadAllText(path);
        var doc = JsonDocument.Parse(json);
        Customers.AddRange(doc.RootElement.GetProperty("customers").EnumerateArray()
            .Select(c => new Customer
            {
                Id = Guid.Parse(c.GetProperty("id").GetString()!),
                Name = c.GetProperty("name").GetString()!,
                Age = c.GetProperty("age").GetInt32(),
                Email = c.GetProperty("email").GetString()!,
                IsActive = c.GetProperty("isActive").GetBoolean(),
                IsVip = c.GetProperty("isVip").GetBoolean(),
                CreatedDate = c.GetProperty("createdDate").GetDateTime(),
                Tags = c.GetProperty("tags").EnumerateArray().Select(t => t.GetString()!).ToList(),
                Orders = c.GetProperty("orders").EnumerateArray().Select(o => new Order
                {
                    Id = o.GetProperty("id").GetInt32(),
                    Total = o.GetProperty("total").GetDouble(),
                    Items = o.GetProperty("items").EnumerateArray().Select(i => i.GetString()!).ToList()
                }).ToList()
            }));
    }

    public static async Task Run(string title, Func<Task> action)
    {
        Section++;
        Console.WriteLine();
        Console.WriteLine($"=== {Section}. {title} ===");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException != null ? $" (Inner: {ex.InnerException.Message})" : "";
            Console.WriteLine($"[ERROR] {ex.Message}{inner}");
        }
        sw.Stop();
        Console.WriteLine($"Completed in {sw.Elapsed.TotalMilliseconds:F2}ms");
    }

    public static void PrintResult(RoslynRules.Models.RuleResult result, string description)
    {
        var color = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
        var status = result.Success ? "PASS" : "FAIL";
        Console.ForegroundColor = color;
        Console.WriteLine($"  [{status}] {description}");
        Console.ResetColor();
    }
}
