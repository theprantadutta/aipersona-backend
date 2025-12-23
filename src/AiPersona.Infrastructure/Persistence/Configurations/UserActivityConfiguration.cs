using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;
using System.Text.Json;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity>
{
    public void Configure(EntityTypeBuilder<UserActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Description).HasMaxLength(1000);

        // Store Metadata as JSON
        builder.Property(a => a.Metadata)
            .HasColumnType("text")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

        // Enum conversions
        builder.Property(a => a.ActivityType).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.TargetType).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.ActivityType);
        builder.HasIndex(a => a.TargetId);
        builder.HasIndex(a => a.CreatedAt).IsDescending();

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
