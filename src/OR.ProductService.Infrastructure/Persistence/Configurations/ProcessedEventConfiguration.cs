using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OR.ProductService.Domain.Entities;

namespace OR.ProductService.Infrastructure.Persistence.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");

        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).ValueGeneratedNever();
        builder.Property(e => e.ProcessedAt).IsRequired();
        
        // Index for efficient cleanup queries
        builder.HasIndex(e => e.ProcessedAt);
    }
}
