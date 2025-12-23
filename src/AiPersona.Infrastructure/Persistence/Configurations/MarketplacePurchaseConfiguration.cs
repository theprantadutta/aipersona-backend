using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class MarketplacePurchaseConfiguration : IEntityTypeConfiguration<MarketplacePurchase>
{
    public void Configure(EntityTypeBuilder<MarketplacePurchase> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(10, 2);

        // Enum conversion
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(p => p.BuyerId);
        builder.HasIndex(p => p.MarketplacePersonaId);
        builder.HasIndex(p => p.PurchasedAt).IsDescending();
    }
}
