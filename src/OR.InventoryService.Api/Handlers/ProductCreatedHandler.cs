using OR.InventoryService.Application.Interfaces;
using OR.InventoryService.Infrastructure.Persistence;
using OR.Shared.Events;
using Wolverine.Attributes;

namespace OR.InventoryService.Api.Handlers;

public static class ProductCreatedHandler
{
    /// <summary>
    /// Handles ProductCreatedEvent using durable inbox with transactional guarantees.
    /// The [Transactional] attribute ensures the message processing, database operations,
    /// and inbox tracking are all committed in a single PostgreSQL transaction.
    /// </summary>
    [Transactional]
    public static async Task HandleAsync(
        ProductCreatedEvent @event,
        InventoryDbContext db,
        IKnownProductRepository knownProductRepository,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation("Received ProductCreatedEvent for product {ProductId}", @event.ProductId);

        await knownProductRepository.AddAsync(@event.ProductId, ct);

        logger.LogInformation("Product {ProductId} is currently in known products read model", @event.ProductId);
    }
}
