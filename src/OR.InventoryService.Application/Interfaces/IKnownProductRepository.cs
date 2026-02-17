namespace OR.InventoryService.Application.Interfaces;

public interface IKnownProductRepository
{
    Task<bool> ExistsAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(Guid productId, CancellationToken ct = default);
}
