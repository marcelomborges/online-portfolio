using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Slug)
            .HasMaxLength(63)
            .IsRequired();

        builder.Property(t => t.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.CustomDomain)
            .HasMaxLength(253);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.HasIndex(t => t.CustomDomain)
            .IsUnique()
            .HasFilter("custom_domain IS NOT NULL");

        builder.HasIndex(t => t.IsActive);

        builder.ToTable(t => t.HasCheckConstraint(
            "chk_tenants_slug_format",
            "slug ~ '^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])?$'"));

        builder.HasOne(t => t.Plan)
            .WithMany(p => p.Tenants)
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
