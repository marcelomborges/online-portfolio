using Microsoft.Extensions.Hosting;

namespace OnlinePortfolio.Api.Data;

public sealed class StartupSeederBackgroundService(
    IServiceProvider services,
    IHostEnvironment env,
    ILogger<StartupSeederBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SeedRolesAsync(stoppingToken);

        if (env.IsDevelopment())
            await DevDataSeeder.SeedAsync(services, stoppingToken);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        // Free-tier DBs (Supabase) can take 30–60 s to wake from pause.
        // Retry with increasing delays before giving up.
        const int maxAttempts = 4;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await IdentityRoleSeeder.SeedAsync(services, ct);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(attempt * 10);
                logger.LogWarning(ex,
                    "Role seeder attempt {Attempt}/{Max} failed — DB may be waking up. Retrying in {Delay}s",
                    attempt, maxAttempts, (int)delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
    }
}
