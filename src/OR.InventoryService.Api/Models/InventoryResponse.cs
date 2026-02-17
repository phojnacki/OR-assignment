namespace OR.InventoryService.Api.Models;

public record InventoryResponse(Guid Id, Guid ProductId, int Quantity, DateTime AddedAt, string AddedBy);
