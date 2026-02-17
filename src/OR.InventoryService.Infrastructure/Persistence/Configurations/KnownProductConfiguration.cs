using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OR.InventoryService.Domain.Entities;

namespace OR.InventoryService.Infrastructure.Persistence.Configurations;

public class KnownProductConfiguration : IEntityTypeConfiguration<KnownProduct>
{
    public void Configure(EntityTypeBuilder<KnownProduct> builder)
    {
        builder.ToTable("known_products");
        builder.HasKey(k => k.ProductId);
        builder.Property(k => k.ProductId).ValueGeneratedNever();
        builder.Property(k => k.ReceivedAt).IsRequired();
    }
}
