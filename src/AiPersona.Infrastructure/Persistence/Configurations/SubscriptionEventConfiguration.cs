using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class SubscriptionEventConfiguration : IEntityTypeConfiguration<SubscriptionEvent>
{
    public void Configure(EntityTypeBuilder<SubscriptionEvent> builder)
    {
        builder.HasKey(se => se.Id);

        builder.Property(se => se.PurchaseToken).HasMaxLength(500).IsRequired();
        builder.Property(se => se.ProductId).HasMaxLength(100).IsRequired();
        builder.Property(se => se.RawResponse).HasColumnType("jsonb");

        // Enum conversions
        builder.Property(se => se.SubscriptionTier).HasConversion<string>().HasMaxLength(50);
        builder.Property(se => se.EventType).HasConversion<string>().HasMaxLength(50);
        builder.Property(se => se.VerificationStatus).HasConversion<string>().HasMaxLength(50);

        // Indexes for subscription history
        builder.HasIndex(se => new { se.UserId, se.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(se => se.PurchaseToken);
        builder.HasIndex(se => se.CreatedAt).IsDescending();
    }
}
