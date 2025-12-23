using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(100).IsRequired();

        // Enum conversion
        builder.Property(a => a.AttachmentType).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(a => a.MessageId);
    }
}
