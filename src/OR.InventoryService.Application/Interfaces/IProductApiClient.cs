namespace OR.InventoryService.Application.Interfaces;

public enum ProductCheckResult
{
    Exists,
    NotFound,
    Unavailable
}

public interface IProductApiClient
{
    Task<ProductCheckResult> CheckProductExistsAsync(Guid productId, CancellationToken ct = default);
}
