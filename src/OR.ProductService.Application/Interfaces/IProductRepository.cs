using OR.ProductService.Domain.Entities;

namespace OR.ProductService.Application.Interfaces;

public interface IProductRepository
{
    void Add(Product product);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task UpdateWithProcessedEventAsync(Product product, Guid processedEventId, CancellationToken ct = default);
    Task<bool> IsEventProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task<int> CleanupOldProcessedEventsAsync(DateTime olderThan, CancellationToken ct = default);
}
