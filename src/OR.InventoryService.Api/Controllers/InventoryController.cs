using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OR.InventoryService.Api.Models;
using OR.InventoryService.Application.Commands;
using OR.InventoryService.Application.Interfaces;
using OR.InventoryService.Infrastructure.Persistence;
using OR.Shared.Events;
using Wolverine.EntityFrameworkCore;

namespace OR.InventoryService.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class InventoryController(
    IInventoryAppService inventoryService,
    IValidator<AddInventoryCommand> validator,
    ILogger<InventoryController> logger) : ControllerBase
{
    private readonly IInventoryAppService _inventoryService = inventoryService;
    private readonly IValidator<AddInventoryCommand> _validator = validator;
    private readonly ILogger<InventoryController> _logger = logger;

    [HttpPost]
    [Authorize(Roles = "write")]
    public async Task<IActionResult> Post(
        [FromBody] AddInventoryRequest request,
        [FromServices] IDbContextOutbox<InventoryDbContext> outbox,
        CancellationToken ct)
    {
        var addedBy = User.Identity?.Name ?? "unknown";
        var command = new AddInventoryCommand(request.ProductId, request.Quantity, addedBy);

        var inventoryId = await _inventoryService.AddInventoryAsync(command, ct);

        await outbox.PublishAsync(new ProductInventoryAddedEvent(inventoryId, request.ProductId, request.Quantity, DateTime.UtcNow));

        // COMMIT
        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        _logger.LogInformation("Inventory entry created with ID {InventoryId}", inventoryId);

        return Created($"/inventory/{inventoryId}", new { Id = inventoryId });
    }
}
