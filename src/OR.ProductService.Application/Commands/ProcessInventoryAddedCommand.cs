namespace OR.ProductService.Application.Commands;

public record ProcessInventoryAddedCommand(Guid EventId, Guid ProductId, int Quantity);
