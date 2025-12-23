using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UserActivityConfiguration : IEntityTypeConfiguration<UserActivity>
{
    public void Configure(EntityTypeBuilder<UserActivity> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TargetId).HasMaxLength(255);
        builder.Property(a => a.TargetType).HasMaxLength(50);
        builder.Property(a => a.ActivityData).HasColumnType("text");

        // Enum conversion
        builder.Property(a => a.ActivityType).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.ActivityType);
        builder.HasIndex(a => a.TargetId);
        builder.HasIndex(a => a.CreatedAt).IsDescending();
    }
}
