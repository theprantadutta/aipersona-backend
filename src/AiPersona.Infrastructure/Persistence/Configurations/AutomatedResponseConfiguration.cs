using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class AutomatedResponseConfiguration : IEntityTypeConfiguration<AutomatedResponse>
{
    public void Configure(EntityTypeBuilder<AutomatedResponse> builder)
    {
        builder.HasKey(ar => ar.Id);

        builder.Property(ar => ar.Title).HasMaxLength(255).IsRequired();
        builder.Property(ar => ar.Content).HasColumnType("text").IsRequired();
        builder.Property(ar => ar.Keywords).HasColumnType("text");

        // Enum conversion
        builder.Property(ar => ar.Category).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(ar => ar.Category);
        builder.HasIndex(ar => ar.IsActive);
        builder.HasIndex(ar => ar.Priority);
    }
}
