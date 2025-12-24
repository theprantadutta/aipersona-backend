using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class PersonaGreetingConfiguration : IEntityTypeConfiguration<PersonaGreeting>
{
    public void Configure(EntityTypeBuilder<PersonaGreeting> builder)
    {
        builder.HasKey(pg => pg.Id);

        // Unique constraint: one greeting per user per persona
        builder.HasIndex(pg => new { pg.UserId, pg.PersonaId }).IsUnique();

        builder.HasIndex(pg => pg.UserId);
        builder.HasIndex(pg => pg.PersonaId);

        builder.Property(pg => pg.GreetingText)
            .IsRequired()
            .HasMaxLength(10000);

        builder.HasOne(pg => pg.User)
            .WithMany()
            .HasForeignKey(pg => pg.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pg => pg.Persona)
            .WithMany()
            .HasForeignKey(pg => pg.PersonaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
