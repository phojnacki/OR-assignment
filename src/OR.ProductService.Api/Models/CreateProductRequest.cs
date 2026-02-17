namespace OR.ProductService.Api.Models;

public record CreateProductRequest(string Name, string? Description, decimal Price);
