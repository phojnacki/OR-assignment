using Microsoft.EntityFrameworkCore;
using OR.ProductService.Application.Interfaces;
using OR.ProductService.Domain.Entities;
using OR.ProductService.Infrastructure.Persistence;

namespace OR.ProductService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _db;

    public ProductRepository(ProductDbContext db) => _db = db;

    public void Add(Product product)
    {
        _db.Products.Add(product);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Products.AsNoTracking().OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
    }

    public async Task UpdateWithProcessedEventAsync(Product product, Guid processedEventId, CancellationToken ct)
    {
        _db.Products.Update(product);
        _db.ProcessedEvents.Add(new ProcessedEvent(processedEventId, DateTime.UtcNow));
    }

    public async Task<bool> IsEventProcessedAsync(Guid eventId, CancellationToken ct)
    {
        return await _db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct);
    }

    public async Task<int> CleanupOldProcessedEventsAsync(DateTime olderThan, CancellationToken ct)
    {
        return await _db.ProcessedEvents
            .Where(e => e.ProcessedAt < olderThan)
            .ExecuteDeleteAsync(ct);
    }
}
