using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(f => f.OriginalName).HasMaxLength(255).IsRequired();
        builder.Property(f => f.MimeType).HasMaxLength(100).IsRequired();

        // Enum conversions
        builder.Property(f => f.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(f => f.ReferenceType).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.Category);
        builder.HasIndex(f => new { f.ReferenceType, f.ReferenceId });
    }
}
