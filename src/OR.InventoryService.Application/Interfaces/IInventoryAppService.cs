using OR.InventoryService.Application.Commands;

namespace OR.InventoryService.Application.Interfaces;

public interface IInventoryAppService
{
    Task<Guid> AddInventoryAsync(AddInventoryCommand command, CancellationToken ct = default);
}
