using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.HasKey(ub => ub.Id);

        builder.Property(ub => ub.Reason).HasMaxLength(500);

        // Unique constraint: one block per blocker-blocked pair
        builder.HasIndex(ub => new { ub.BlockerId, ub.BlockedId }).IsUnique();

        builder.HasIndex(ub => ub.BlockerId);
        builder.HasIndex(ub => ub.BlockedId);

        // Relationships
        builder.HasOne(ub => ub.Blocker)
            .WithMany(u => u.BlockedUsers)
            .HasForeignKey(ub => ub.BlockerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ub => ub.Blocked)
            .WithMany(u => u.BlockedByUsers)
            .HasForeignKey(ub => ub.BlockedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
