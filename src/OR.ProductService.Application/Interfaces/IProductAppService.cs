using OR.ProductService.Application.Commands;
using OR.ProductService.Application.DTOs;

namespace OR.ProductService.Application.Interfaces;

public interface IProductAppService
{
    Task<Guid> CreateProductAsync(CreateProductCommand command);
    Task<ProductDto?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ProductDto>> GetAllProductsAsync(CancellationToken ct = default);
    Task ProcessInventoryAddedAsync(ProcessInventoryAddedCommand command, CancellationToken ct = default);
}
