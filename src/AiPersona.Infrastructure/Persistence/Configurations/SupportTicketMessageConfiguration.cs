using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).HasColumnType("text").IsRequired();
        builder.Property(m => m.Attachments).HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(m => m.TicketId);
        builder.HasIndex(m => m.CreatedAt);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
