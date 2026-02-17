using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OR.ProductService.Api.Models;
using OR.ProductService.Application.Commands;
using OR.ProductService.Application.Interfaces;
using OR.ProductService.Infrastructure.Persistence;
using OR.Shared.Events;
using Wolverine.EntityFrameworkCore;

namespace OR.ProductService.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductsController(
    IProductAppService productService,
    IValidator<CreateProductCommand> validator,
    ILogger<ProductsController> logger) : ControllerBase
{
    private readonly IProductAppService _productService = productService;
    private readonly IValidator<CreateProductCommand> _validator = validator;
    private readonly ILogger<ProductsController> _logger = logger;

    [HttpPost]
    [Authorize(Roles = "write")]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        [FromServices] IDbContextOutbox<ProductDbContext> outbox,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Description ?? string.Empty, request.Price);
        
        var id = await _productService.CreateProductAsync(command);

        await outbox.PublishAsync(new ProductCreatedEvent(id, DateTime.UtcNow));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        _logger.LogInformation("Product created with ID {ProductId}", id);
        return CreatedAtAction(nameof(GetAll), new { id });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var productDto = await _productService.GetProductByIdAsync(id, ct);
        if (productDto is null)
            return NotFound();

        var response = new ProductResponse(
            productDto.Id,
            productDto.Name,
            productDto.Description,
            productDto.Price,
            productDto.Amount,
            productDto.CreatedAt,
            productDto.UpdatedAt);

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "read")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var productDtos = await _productService.GetAllProductsAsync(ct);
        var responses = productDtos.Select(dto => new ProductResponse(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.Price,
            dto.Amount,
            dto.CreatedAt,
            dto.UpdatedAt)).ToList();

        return Ok(responses);
    }
}
