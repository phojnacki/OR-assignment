using Microsoft.EntityFrameworkCore;
using OR.InventoryService.Application.Interfaces;
using OR.InventoryService.Domain.Entities;
using OR.InventoryService.Infrastructure.Persistence;

namespace OR.InventoryService.Infrastructure.Repositories;

public class KnownProductRepository(InventoryDbContext db) : IKnownProductRepository
{
    private readonly InventoryDbContext _db = db;

    public async Task<bool> ExistsAsync(Guid productId, CancellationToken ct)
    {
        return await _db.KnownProducts.AnyAsync(k => k.ProductId == productId, ct);
    }

    public async Task AddAsync(Guid productId, CancellationToken ct)
    {
        if (!await ExistsAsync(productId, ct))
        {
            _db.KnownProducts.Add(new KnownProduct(productId, DateTime.UtcNow));
            // Note: SaveChangesAsync is not called here when used in a transactional context
            // The transaction is managed by the caller (e.g., Wolverine's [Transactional] attribute)
        }
    }
}
