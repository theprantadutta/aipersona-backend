using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class FcmTokenConfiguration : IEntityTypeConfiguration<FcmToken>
{
    public void Configure(EntityTypeBuilder<FcmToken> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Token).HasMaxLength(500).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();

        builder.Property(t => t.DeviceId).HasMaxLength(255).IsRequired();
        builder.HasIndex(t => t.DeviceId);

        // Enum conversion
        builder.Property(t => t.Platform).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.IsActive);
    }
}
