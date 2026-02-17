namespace OR.ProductService.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Amount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Product() { }

    public static Product Create(string name, string description, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description ?? string.Empty,
            Price = price,
            Amount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void IncreaseAmount(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        Amount += quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
