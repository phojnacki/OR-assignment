namespace OR.Shared.Events;

public record ProductCreatedEvent(Guid ProductId, DateTime OccurredAt);
