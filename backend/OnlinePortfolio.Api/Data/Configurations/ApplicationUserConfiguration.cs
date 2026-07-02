using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.Email)
            .HasMaxLength(320);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(u => u.TenantId);

        builder.HasIndex(u => new { u.TenantId, u.Email })
            .IsUnique()
            .HasFilter("tenant_id IS NOT NULL AND email IS NOT NULL");

        builder.HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.InvitedByUser)
            .WithMany()
            .HasForeignKey(u => u.InvitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
