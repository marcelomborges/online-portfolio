using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.MaxStorageMb)
            .IsRequired();

        builder.Property(p => p.MaxArtworks)
            .IsRequired();

        builder.Property(p => p.CustomDomainAllowed)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
