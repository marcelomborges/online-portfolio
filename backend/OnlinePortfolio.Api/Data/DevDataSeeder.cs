using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data;

public static class DevDataSeeder
{
    // Fixed IDs so local resets always produce the same rows.
    internal static readonly Guid StarterPlanId  = new("00000000-0000-0000-0000-000000000001");
    internal static readonly Guid TenantAnaId    = new("00000000-0000-0000-0000-000000000010");
    internal static readonly Guid TenantJoaoId   = new("00000000-0000-0000-0000-000000000011");

    private record TenantSeed(Guid Id, string Slug, string DisplayName);

    private static readonly TenantSeed[] Tenants =
    [
        new(TenantAnaId,  "ana",  "Ana Silva"),
        new(TenantJoaoId, "joao", "João Santos"),
    ];

    // Explicit dependencies overload — lets unit tests inject an in-memory DB + mock UserManager.
    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        string adminEmail,
        string adminPassword,
        ILogger logger,
        CancellationToken ct = default)
    {
        // --- Plan ---
        if (!await db.Plans.AnyAsync(ct))
        {
            db.Plans.Add(new Plan
            {
                Id = StarterPlanId,
                Name = "Starter",
                MaxStorageMb = 500,
                MaxArtworks = 50,
                CustomDomainAllowed = false,
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Dev seed: plan 'Starter' created");
        }

        var plan = await db.Plans.FirstAsync(ct);

        // --- Tenants + TenantSettings ---
        foreach (var seed in Tenants)
        {
            if (await db.Tenants.AnyAsync(t => t.Slug == seed.Slug, ct))
                continue;

            db.Tenants.Add(new Tenant
            {
                Id = seed.Id,
                PlanId = plan.Id,
                Slug = seed.Slug,
                DisplayName = seed.DisplayName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            db.TenantSettings.Add(new TenantSettings
            {
                TenantId = seed.Id,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
            logger.LogInformation("Dev seed: tenant '{Slug}' created", seed.Slug);
        }

        await db.SaveChangesAsync(ct);

        // --- PlatformAdmin user ---
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            IsActive = true,
            TenantId = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Dev seed: failed to create PlatformAdmin '{adminEmail}': {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.PlatformAdmin);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Dev seed: failed to assign PlatformAdmin role to '{adminEmail}': {errors}");
        }

        logger.LogInformation("Dev seed: PlatformAdmin '{Email}' created", adminEmail);
    }

    // Public entry point called from Program.cs.
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken ct = default)
    {
        var adminEmail = configuration["DevSeed:PlatformAdminEmail"]
            ?? throw new InvalidOperationException("DevSeed:PlatformAdminEmail is required.");
        var adminPassword = configuration["DevSeed:PlatformAdminPassword"]
            ?? throw new InvalidOperationException("DevSeed:PlatformAdminPassword is required.");

        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevDataSeeder));

        await SeedAsync(db, userManager, adminEmail, adminPassword, logger, ct);
    }
}
