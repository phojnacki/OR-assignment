namespace OR.ProductService.Application.Exceptions;

public class ProductNotFoundException(Guid productId, Guid eventId) : Exception($"Product {productId} not found while processing event {eventId}")
{
    public Guid ProductId { get; } = productId;
    public Guid EventId { get; } = eventId;
}
