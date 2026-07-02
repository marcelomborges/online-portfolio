using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace OnlinePortfolio.Api.Data;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var roleName in AppRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            try
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed role '{roleName}': {errors}");
                }

                logger.LogInformation("Seeded Identity role {Role}", roleName);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is PostgresException { SqlState: "23505" })
            {
                // INSERT succeeded on the DB side but the connection timed out before
                // the response arrived; EnableRetryOnFailure retried with the same Guid
                // and hit the PK constraint. The role exists — desired state reached.
                logger.LogInformation("Role {Role} already exists (concurrent seed), skipping", roleName);
            }
        }
    }

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IdentityRoleSeeder));

        await SeedAsync(roleManager, logger, cancellationToken);
    }
}
