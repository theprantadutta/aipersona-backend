using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class PersonaConfiguration : IEntityTypeConfiguration<Persona>
{
    public void Configure(EntityTypeBuilder<Persona> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Description).HasColumnType("text");
        builder.Property(p => p.ImagePath).HasMaxLength(500);
        builder.Property(p => p.Bio).HasColumnType("text");
        builder.Property(p => p.LanguageStyle).HasMaxLength(100);
        builder.Property(p => p.VoiceId).HasMaxLength(100);
        builder.Property(p => p.VoiceSettings).HasColumnType("text");

        // JSON columns
        builder.Property(p => p.PersonalityTraits).HasColumnType("jsonb");
        builder.Property(p => p.Expertise).HasColumnType("jsonb");
        builder.Property(p => p.Tags).HasColumnType("jsonb");

        // Enum conversion
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);

        // Indexes for discovery and performance
        builder.HasIndex(p => p.CreatorId);
        builder.HasIndex(p => new { p.IsPublic, p.Status });
        builder.HasIndex(p => p.LikeCount).IsDescending();
        builder.HasIndex(p => p.IsMarketplace);
        builder.HasIndex(p => p.CreatedAt).IsDescending();

        // Self-referential relationship for cloning
        builder.HasOne(p => p.ClonedFromPersona)
            .WithMany()
            .HasForeignKey(p => p.ClonedFromPersonaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.OriginalCreator)
            .WithMany()
            .HasForeignKey(p => p.OriginalCreatorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.KnowledgeBases)
            .WithOne(kb => kb.Persona)
            .HasForeignKey(kb => kb.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ChatSessions)
            .WithOne(cs => cs.Persona)
            .HasForeignKey(cs => cs.PersonaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Likes)
            .WithOne(l => l.Persona)
            .HasForeignKey(l => l.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Favorites)
            .WithOne(f => f.Persona)
            .HasForeignKey(f => f.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Views)
            .WithOne(v => v.Persona)
            .HasForeignKey(v => v.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
