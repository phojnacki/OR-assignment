namespace OR.ProductService.Api.Models;

public record ProductResponse(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Amount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
