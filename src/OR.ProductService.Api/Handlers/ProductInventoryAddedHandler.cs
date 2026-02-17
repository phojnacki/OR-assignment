using OR.ProductService.Application.Commands;
using OR.ProductService.Application.Interfaces;
using OR.Shared.Events;
using Wolverine.Attributes;

namespace OR.ProductService.Api.Handlers;

public static class ProductInventoryAddedHandler
{
    /// <summary>
    /// Handles ProductInventoryAddedEvent using durable inbox with transactional guarantees.
    /// The [Transactional] attribute ensures the message processing, database operations,
    /// and inbox tracking are all committed in a single transaction.
    /// </summary>
    [Transactional]
    public static async Task HandleAsync(
        ProductInventoryAddedEvent @event,
        IProductAppService productAppService,
        ILogger logger,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Received ProductInventoryAddedEvent: EventId={EventId}, ProductId={ProductId}, Quantity={Quantity}",
            @event.EventId, @event.ProductId, @event.Quantity);

        var command = new ProcessInventoryAddedCommand(@event.EventId, @event.ProductId, @event.Quantity);
        await productAppService.ProcessInventoryAddedAsync(command, ct);

        // No need to call SaveChangesAsync() - Wolverine's transactional middleware
        // will automatically save all DbContext changes and commit the inbox entry in a single transaction

        logger.LogInformation("Successfully processed event {EventId}", @event.EventId);
    }
}
