using OR.InventoryService.Domain.Entities;

namespace OR.InventoryService.Application.Interfaces;

public interface IInventoryRepository
{
    void Add(Inventory inventory);
}
