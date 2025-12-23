using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // Email - unique index
        builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(255);

        // Firebase UID - unique filtered index
        builder.Property(u => u.FirebaseUid).HasMaxLength(255);
        builder.HasIndex(u => u.FirebaseUid).IsUnique().HasFilter("firebase_uid IS NOT NULL");

        // Google ID - unique filtered index
        builder.Property(u => u.GoogleId).HasMaxLength(255);
        builder.HasIndex(u => u.GoogleId).IsUnique().HasFilter("google_id IS NOT NULL");

        builder.Property(u => u.DisplayName).HasMaxLength(255);
        builder.Property(u => u.PhotoUrl).HasColumnType("text");
        builder.Property(u => u.Bio).HasMaxLength(500);
        builder.Property(u => u.GooglePlayPurchaseToken).HasMaxLength(500);

        // Enum conversions stored as strings
        builder.Property(u => u.AuthProvider).HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.SubscriptionTier).HasConversion<string>().HasMaxLength(50);

        // Indexes for query performance
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.SubscriptionTier);
        builder.HasIndex(u => u.CreatedAt);

        // Relationships
        builder.HasOne(u => u.UsageTracking)
            .WithOne(ut => ut.User)
            .HasForeignKey<UsageTracking>(ut => ut.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Personas)
            .WithOne(p => p.Creator)
            .HasForeignKey(p => p.CreatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ChatSessions)
            .WithOne(cs => cs.User)
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SubscriptionEvents)
            .WithOne(se => se.User)
            .HasForeignKey(se => se.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.FcmTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.UploadedFiles)
            .WithOne(f => f.User)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.MarketplacePurchases)
            .WithOne(p => p.Buyer)
            .HasForeignKey(p => p.BuyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PersonaLikes)
            .WithOne(pl => pl.User)
            .HasForeignKey(pl => pl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PersonaFavorites)
            .WithOne(pf => pf.User)
            .HasForeignKey(pf => pf.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Reports)
            .WithOne(r => r.Reporter)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Activities)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SupportTickets)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
