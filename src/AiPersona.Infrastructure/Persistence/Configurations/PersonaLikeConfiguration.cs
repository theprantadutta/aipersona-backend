using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class PersonaLikeConfiguration : IEntityTypeConfiguration<PersonaLike>
{
    public void Configure(EntityTypeBuilder<PersonaLike> builder)
    {
        builder.HasKey(pl => pl.Id);

        // Unique constraint: one like per user per persona
        builder.HasIndex(pl => new { pl.UserId, pl.PersonaId }).IsUnique();

        builder.HasIndex(pl => pl.UserId);
        builder.HasIndex(pl => pl.PersonaId);
    }
}
