namespace OR.InventoryService.Domain.Entities;

public class Inventory
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime AddedAt { get; private set; }
    public string AddedBy { get; private set; } = string.Empty;

    private Inventory() { }

    public static Inventory Create(Guid productId, int quantity, string addedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentException.ThrowIfNullOrWhiteSpace(addedBy);

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId must not be empty.", nameof(productId));

        return new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = quantity,
            AddedAt = DateTime.UtcNow,
            AddedBy = addedBy
        };
    }
}
