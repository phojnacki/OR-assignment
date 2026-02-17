namespace OR.ProductService.Domain.Entities;

public record ProcessedEvent(Guid EventId, DateTime ProcessedAt);
