using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class MarketplacePersonaConfiguration : IEntityTypeConfiguration<MarketplacePersona>
{
    public void Configure(EntityTypeBuilder<MarketplacePersona> builder)
    {
        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.Title).HasMaxLength(255).IsRequired();
        builder.Property(mp => mp.Description).HasColumnType("text").IsRequired();
        builder.Property(mp => mp.Category).HasMaxLength(100).IsRequired();
        builder.Property(mp => mp.Price).HasPrecision(10, 2);

        // Enum conversions
        builder.Property(mp => mp.PricingType).HasConversion<string>().HasMaxLength(50);
        builder.Property(mp => mp.Status).HasConversion<string>().HasMaxLength(50);

        // Unique constraint: one listing per persona
        builder.HasIndex(mp => mp.PersonaId).IsUnique();
        builder.HasIndex(mp => mp.SellerId);
        builder.HasIndex(mp => mp.Status);
        builder.HasIndex(mp => mp.Category);

        // Relationship to Persona
        builder.HasOne(mp => mp.Persona)
            .WithOne(p => p.MarketplaceListing)
            .HasForeignKey<MarketplacePersona>(mp => mp.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mp => mp.Purchases)
            .WithOne(p => p.MarketplacePersona)
            .HasForeignKey(p => p.MarketplacePersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(mp => mp.Reviews)
            .WithOne(r => r.MarketplacePersona)
            .HasForeignKey(r => r.MarketplacePersonaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
