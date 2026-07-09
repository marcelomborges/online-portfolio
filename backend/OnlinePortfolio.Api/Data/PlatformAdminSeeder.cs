using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlinePortfolio.Api.Data.Entities;
using OnlinePortfolio.Api.Options;

namespace OnlinePortfolio.Api.Data;

public static class PlatformAdminSeeder
{
    /// <summary>
    /// Creates the PlatformAdmin user when AdminSeed__Email + AdminSeed__Password are configured.
    /// No-op if either is missing. Safe to call on every startup (idempotent).
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var opts = sp.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        if (string.IsNullOrWhiteSpace(opts.Email) || string.IsNullOrWhiteSpace(opts.Password))
            return;

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger      = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PlatformAdminSeeder));

        if (await userManager.FindByEmailAsync(opts.Email) is not null)
        {
            logger.LogInformation("PlatformAdmin '{Email}' already exists — skipping seed", opts.Email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName       = opts.Email,
            Email          = opts.Email,
            EmailConfirmed = true,
            IsActive       = true,
            TenantId       = null,
            CreatedAt      = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(admin, opts.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"PlatformAdmin seed failed for '{opts.Email}': {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.PlatformAdmin);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Failed to assign PlatformAdmin role to '{opts.Email}': {errors}");
        }

        logger.LogInformation("PlatformAdmin '{Email}' seeded", opts.Email);
    }
}
