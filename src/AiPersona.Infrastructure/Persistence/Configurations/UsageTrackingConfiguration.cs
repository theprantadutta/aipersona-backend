using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AiPersona.Domain.Entities;

namespace AiPersona.Infrastructure.Persistence.Configurations;

public class UsageTrackingConfiguration : IEntityTypeConfiguration<UsageTracking>
{
    public void Configure(EntityTypeBuilder<UsageTracking> builder)
    {
        builder.HasKey(ut => ut.Id);

        builder.HasIndex(ut => ut.UserId).IsUnique();
    }
}
