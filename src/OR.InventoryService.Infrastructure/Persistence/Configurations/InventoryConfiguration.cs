using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OR.InventoryService.Domain.Entities;

namespace OR.InventoryService.Infrastructure.Persistence.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("inventories");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();
        builder.Property(i => i.AddedAt).IsRequired();
        builder.Property(i => i.AddedBy).IsRequired().HasMaxLength(200);

        builder.HasIndex(i => i.ProductId);
    }
}
