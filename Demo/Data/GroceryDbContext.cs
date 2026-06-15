using Microsoft.EntityFrameworkCore;

namespace Demo.Data;

public class GroceryDbContext : DbContext
{
    public DbSet<GroceryItem> GroceryItems { get; set; } = null!;
    public DbSet<GroceryList> GroceryLists { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseInMemoryDatabase("GroceryDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroceryItem>().HasKey(g => g.Id);
        modelBuilder.Entity<GroceryList>().HasKey(g => g.Id);
        modelBuilder.Entity<GroceryList>()
            .OwnsMany(l => l.Items, item =>
            {
                item.HasKey(i => i.Id);
                item.WithOwner().HasForeignKey("GroceryListId");
            });
    }
}

public class GroceryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool InStock { get; set; }
}

public class GroceryList
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public List<GroceryListItem> Items { get; set; } = new();
}

public class GroceryListItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool Purchased { get; set; }
}
