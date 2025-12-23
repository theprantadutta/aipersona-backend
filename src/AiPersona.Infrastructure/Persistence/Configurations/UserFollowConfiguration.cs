using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.HasKey(uf => uf.Id);

        // Unique constraint: one follow per follower-following pair
        builder.HasIndex(uf => new { uf.FollowerId, uf.FollowingId }).IsUnique();

        builder.HasIndex(uf => uf.FollowerId);
        builder.HasIndex(uf => uf.FollowingId);

        // Relationships
        builder.HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
