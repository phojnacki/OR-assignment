using Microsoft.Extensions.Logging;
using OR.InventoryService.Application.Commands;
using OR.InventoryService.Application.Interfaces;
using OR.InventoryService.Domain.Entities;
using OR.Shared.Events;

namespace OR.InventoryService.Application.Services;

public class InventoryAppService(
    IInventoryRepository repository,
    IKnownProductRepository knownProducts,
    IProductApiClient productApiClient,
    ILogger<InventoryAppService> logger) : IInventoryAppService
{
    private readonly IInventoryRepository _repository = repository;
    private readonly IKnownProductRepository _knownProducts = knownProducts;
    private readonly IProductApiClient _productApiClient = productApiClient;
    private readonly ILogger<InventoryAppService> _logger = logger;

    public async Task<Guid> AddInventoryAsync(AddInventoryCommand command, CancellationToken ct = default)
    {
        await EnsureProductExistsAsync(command.ProductId, ct);
        
        var inventory = Inventory.Create(command.ProductId, command.Quantity, command.AddedBy);
        _repository.Add(inventory);
        
        return inventory.Id;
    }

    private async Task EnsureProductExistsAsync(Guid productId, CancellationToken ct)
    {
        // 1. Fast path: check local read model
        if (await _knownProducts.ExistsAsync(productId, ct))
            return;

        // 2. Slow path: HTTP fallback for race conditions (event not yet received)
        _logger.LogInformation(
            "Product {ProductId} not in local read model, checking ProductService via HTTP",
            productId);

        var result = await _productApiClient.CheckProductExistsAsync(productId, ct);

        if (result == ProductCheckResult.Exists)
        {
            // Cache locally so future checks are fast
            await _knownProducts.AddAsync(productId, ct);
            return;
        }

        throw new ArgumentException(
            $"Register the product with id {productId} first before adding the inventory.");
    }
}
