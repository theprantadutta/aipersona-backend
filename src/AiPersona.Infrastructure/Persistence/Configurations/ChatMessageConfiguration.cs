using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Text).HasColumnType("text").IsRequired();
        builder.Property(m => m.Sentiment).HasMaxLength(50);
        builder.Property(m => m.MetaData).HasColumnType("jsonb");

        // Enum conversions
        builder.Property(m => m.SenderType).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.MessageType).HasConversion<string>().HasMaxLength(50);

        // Indexes for message pagination
        builder.HasIndex(m => new { m.SessionId, m.CreatedAt });
        builder.HasIndex(m => m.CreatedAt);

        builder.HasMany(m => m.Attachments)
            .WithOne(a => a.Message)
            .HasForeignKey(a => a.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
