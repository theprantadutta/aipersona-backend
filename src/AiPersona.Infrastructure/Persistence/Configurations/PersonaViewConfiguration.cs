using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class PersonaViewConfiguration : IEntityTypeConfiguration<PersonaView>
{
    public void Configure(EntityTypeBuilder<PersonaView> builder)
    {
        builder.HasKey(pv => pv.Id);

        builder.HasIndex(pv => pv.PersonaId);
        builder.HasIndex(pv => pv.UserId);
        builder.HasIndex(pv => pv.ViewedAt);

        builder.HasOne(pv => pv.User)
            .WithMany()
            .HasForeignKey(pv => pv.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
