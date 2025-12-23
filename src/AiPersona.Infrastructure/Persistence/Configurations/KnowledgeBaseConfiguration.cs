using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class KnowledgeBaseConfiguration : IEntityTypeConfiguration<KnowledgeBase>
{
    public void Configure(EntityTypeBuilder<KnowledgeBase> builder)
    {
        builder.HasKey(kb => kb.Id);

        builder.Property(kb => kb.SourceName).HasMaxLength(255);
        builder.Property(kb => kb.Content).HasColumnType("text").IsRequired();
        builder.Property(kb => kb.MetaData).HasColumnType("jsonb");

        // Enum conversions
        builder.Property(kb => kb.SourceType).HasConversion<string>().HasMaxLength(50);
        builder.Property(kb => kb.Status).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(kb => kb.PersonaId);
        builder.HasIndex(kb => kb.Status);
    }
}
