namespace OR.InventoryService.Api.Models;

public record AddInventoryRequest(Guid ProductId, int Quantity);
