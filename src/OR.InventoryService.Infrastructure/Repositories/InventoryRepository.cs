using OR.InventoryService.Application.Interfaces;
using OR.InventoryService.Domain.Entities;
using OR.InventoryService.Infrastructure.Persistence;

namespace OR.InventoryService.Infrastructure.Repositories;

public class InventoryRepository(InventoryDbContext db) : IInventoryRepository
{
    private readonly InventoryDbContext _db = db;

    public void Add(Inventory inventory)
    {
        _db.Inventories.Add(inventory);
    }
}
