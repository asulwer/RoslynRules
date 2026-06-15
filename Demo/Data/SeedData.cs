using Demo.Data;
using Microsoft.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore.Entities;

namespace Demo.Data;

public static class SeedData
{
    public static async Task InitializeAsync()
    {
        await using var rulesDb = new RulesDbContext();
        await using var groceryDb = new GroceryDbContext();

        await SeedRulesAsync(rulesDb);
        await SeedGroceryAsync(groceryDb);
    }

    private static async Task SeedRulesAsync(RulesDbContext db)
    {
        if (await db.Workflows.AnyAsync()) return;

        var workflow = new WorkflowEntity
        {
            Description = "Grocery validation rules",
            Rules = new List<RuleEntity>
            {
                new()
                {
                    Description = "Has perishable items",
                    Expression = "items.Any(i => i.Category == \"Dairy\" || i.Category == \"Produce\")"
                },
                new()
                {
                    Description = "Under $30 budget",
                    Expression = "items.Sum(i => i.Price) <= 30m"
                },
                new()
                {
                    Description = "Quality checks",
                    Expression = "items.All(i => i.Price > 0)",
                    ChildRules = new List<RuleEntity>
                    {
                        new()
                        {
                            Description = "No out of stock",
                            Expression = "items.All(i => i.InStock == true)"
                        },
                        new()
                        {
                            Description = "Has dairy",
                            Expression = "items.Any(i => i.Category == \"Dairy\")",
                            ChildRules = new List<RuleEntity>
                            {
                                new()
                                {
                                    Description = "Fresh milk check",
                                    Expression = "items.Any(i => i.Name == \"Milk\" && i.InStock)"
                                }
                            }
                        }
                    }
                }
            }
        };

        db.Workflows.Add(workflow);
        await db.SaveChangesAsync();
    }

    private static async Task SeedGroceryAsync(GroceryDbContext db)
    {
        if (await db.GroceryItems.AnyAsync()) return;

        await db.GroceryItems.AddRangeAsync(
            new GroceryItem { Name = "Milk", Category = "Dairy", Price = 3.49m, InStock = true },
            new GroceryItem { Name = "Bread", Category = "Bakery", Price = 2.99m, InStock = true },
            new GroceryItem { Name = "Eggs", Category = "Dairy", Price = 4.29m, InStock = true },
            new GroceryItem { Name = "Cheese", Category = "Dairy", Price = 5.99m, InStock = false },
            new GroceryItem { Name = "Apples", Category = "Produce", Price = 1.99m, InStock = true },
            new GroceryItem { Name = "Chicken", Category = "Meat", Price = 8.99m, InStock = true },
            new GroceryItem { Name = "Rice", Category = "Pantry", Price = 3.79m, InStock = true },
            new GroceryItem { Name = "Coffee", Category = "Beverages", Price = 7.99m, InStock = false }
        );

        await db.GroceryLists.AddRangeAsync(
            new GroceryList
            {
                Name = "Weekly Shopping",
                Items = new List<GroceryListItem>
                {
                    new() { ItemName = "Milk", Quantity = 1 },
                    new() { ItemName = "Bread", Quantity = 2 },
                    new() { ItemName = "Eggs", Quantity = 1 },
                    new() { ItemName = "Apples", Quantity = 5 }
                }
            },
            new GroceryList
            {
                Name = "Party Prep",
                Items = new List<GroceryListItem>
                {
                    new() { ItemName = "Cheese", Quantity = 3 },
                    new() { ItemName = "Chicken", Quantity = 2 },
                    new() { ItemName = "Rice", Quantity = 1 },
                    new() { ItemName = "Coffee", Quantity = 2 }
                }
            }
        );

        await db.SaveChangesAsync();
    }
}
