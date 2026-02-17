namespace OR.ProductService.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Amount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
