namespace OR.InventoryService.Api.Models;

public record TokenRequest(string Username, string Password, string[] Roles);
