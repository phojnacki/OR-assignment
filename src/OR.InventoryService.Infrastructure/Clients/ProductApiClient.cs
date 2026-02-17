using Microsoft.Extensions.Logging;
using OR.InventoryService.Application.Interfaces;

namespace OR.InventoryService.Infrastructure.Clients;

public class ProductApiClient(HttpClient httpClient, ILogger<ProductApiClient> logger) : IProductApiClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<ProductApiClient> _logger = logger;

    public async Task<ProductCheckResult> CheckProductExistsAsync(Guid productId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/products/{productId}", ct);

            if (response.IsSuccessStatusCode)
                return ProductCheckResult.Exists;

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return ProductCheckResult.NotFound;

            _logger.LogWarning(
                "Unexpected status {StatusCode} checking product {ProductId}",
                response.StatusCode, productId);
            return ProductCheckResult.Unavailable;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "ProductService unavailable while checking product {ProductId}", productId);
            return ProductCheckResult.Unavailable;
        }
    }
}
