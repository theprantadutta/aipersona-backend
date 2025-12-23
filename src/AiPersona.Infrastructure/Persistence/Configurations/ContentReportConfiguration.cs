using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class ContentReportConfiguration : IEntityTypeConfiguration<ContentReport>
{
    public void Configure(EntityTypeBuilder<ContentReport> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ContentId).HasMaxLength(255).IsRequired();
        builder.Property(r => r.AdditionalInfo).HasColumnType("text");
        builder.Property(r => r.Resolution).HasColumnType("text");

        // Enum conversions
        builder.Property(r => r.ContentType).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Reason).HasConversion<string>().HasMaxLength(50);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(r => r.ReporterId);
        builder.HasIndex(r => r.ContentId);
        builder.HasIndex(r => r.ContentType);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAt).IsDescending();
    }
}
