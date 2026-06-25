using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Configurations;

public sealed class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.HasKey(s => s.TenantId);

        builder.Property(s => s.ContactEmail)
            .HasMaxLength(320);

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasOne(s => s.Tenant)
            .WithOne(t => t.Settings)
            .HasForeignKey<TenantSettings>(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
