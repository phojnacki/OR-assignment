using Microsoft.Extensions.Logging;
using OR.ProductService.Application.Commands;
using OR.ProductService.Application.DTOs;
using OR.ProductService.Application.Exceptions;
using OR.ProductService.Application.Interfaces;
using OR.ProductService.Domain.Entities;

namespace OR.ProductService.Application.Services;

public class ProductAppService : IProductAppService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductAppService> _logger;

    public ProductAppService(
        IProductRepository repository,
        ILogger<ProductAppService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> CreateProductAsync(CreateProductCommand command)
    {
        var product = Product.Create(command.Name, command.Description ?? string.Empty, command.Price);
        _repository.Add(product);

        _logger.LogInformation("Product {ProductId} created: {ProductName}", product.Id, product.Name);
        return product.Id;
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        return product is null
            ? null
            : new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Amount, product.CreatedAt, product.UpdatedAt);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllProductsAsync(CancellationToken ct)
    {
        var products = await _repository.GetAllAsync(ct);
        return products.Select(p => new ProductDto(
            p.Id, p.Name, p.Description, p.Price, p.Amount, p.CreatedAt, p.UpdatedAt
        )).ToList();
    }

    public async Task ProcessInventoryAddedAsync(ProcessInventoryAddedCommand command, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing inventory added: EventId={EventId}, ProductId={ProductId}, Quantity={Quantity}",
            command.EventId, command.ProductId, command.Quantity);

        // Check if event was already processed (application-level idempotency)
        var isProcessed = await _repository.IsEventProcessedAsync(command.EventId, ct);
        if (isProcessed)
        {
            _logger.LogWarning("Event {EventId} already processed, skipping duplicate", command.EventId);
            return;
        }

        // Load the product
        var product = await _repository.GetByIdAsync(command.ProductId, ct);
        if (product is null)
        {
            throw new ProductNotFoundException(command.ProductId, command.EventId);
        }

        // Update product amount
        product.IncreaseAmount(command.Quantity);

        // Update product and track event as processed (repository will handle both)
        await _repository.UpdateWithProcessedEventAsync(product, command.EventId, ct);

        _logger.LogInformation("Successfully processed event {EventId}", command.EventId);
    }

}
