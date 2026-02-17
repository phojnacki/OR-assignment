namespace OR.InventoryService.Application.Commands;

public record AddInventoryCommand(Guid ProductId, int Quantity, string AddedBy);
