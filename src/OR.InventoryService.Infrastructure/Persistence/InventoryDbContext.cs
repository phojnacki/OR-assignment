using Microsoft.EntityFrameworkCore;
using OR.InventoryService.Domain.Entities;

namespace OR.InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<KnownProduct> KnownProducts => Set<KnownProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
