using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.Property(cs => cs.PersonaName).HasMaxLength(255).IsRequired();
        builder.Property(cs => cs.DeletedPersonaName).HasMaxLength(255);
        builder.Property(cs => cs.DeletedPersonaImage).HasMaxLength(500);
        builder.Property(cs => cs.MetaData).HasColumnType("jsonb");

        // Enum conversion
        builder.Property(cs => cs.Status).HasConversion<string>().HasMaxLength(50);

        // Indexes for chat list and performance
        builder.HasIndex(cs => new { cs.UserId, cs.Status });
        builder.HasIndex(cs => new { cs.UserId, cs.IsPinned, cs.LastMessageAt }).IsDescending(false, false, true);
        builder.HasIndex(cs => cs.PersonaId);
        builder.HasIndex(cs => cs.LastMessageAt).IsDescending();

        builder.HasMany(cs => cs.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
