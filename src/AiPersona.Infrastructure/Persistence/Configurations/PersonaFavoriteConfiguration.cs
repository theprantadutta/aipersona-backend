using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class PersonaFavoriteConfiguration : IEntityTypeConfiguration<PersonaFavorite>
{
    public void Configure(EntityTypeBuilder<PersonaFavorite> builder)
    {
        builder.HasKey(pf => pf.Id);

        // Unique constraint: one favorite per user per persona
        builder.HasIndex(pf => new { pf.UserId, pf.PersonaId }).IsUnique();

        builder.HasIndex(pf => pf.UserId);
        builder.HasIndex(pf => pf.PersonaId);
    }
}
