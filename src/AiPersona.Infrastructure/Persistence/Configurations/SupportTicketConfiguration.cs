using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Subject).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Description).HasColumnType("text").IsRequired();

        // Enum conversions
        builder.Property(t => t.Category).HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.Priority).HasConversion<string>().HasMaxLength(50);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(50);

        // Indexes
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.AssignedTo);
        builder.HasIndex(t => t.CreatedAt).IsDescending();

        builder.HasOne(t => t.AssignedAgent)
            .WithMany()
            .HasForeignKey(t => t.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Messages)
            .WithOne(m => m.Ticket)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
