using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class MarketplaceReviewConfiguration : IEntityTypeConfiguration<MarketplaceReview>
{
    public void Configure(EntityTypeBuilder<MarketplaceReview> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReviewText).HasColumnType("text");

        // Indexes
        builder.HasIndex(r => r.MarketplacePersonaId);
        builder.HasIndex(r => r.ReviewerId);
        builder.HasIndex(r => r.Rating);

        // Unique constraint: one review per user per persona
        builder.HasIndex(r => new { r.MarketplacePersonaId, r.ReviewerId }).IsUnique();
    }
}
