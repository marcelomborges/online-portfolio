using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Invite-only: email is confirmed by the accept-invite flow, not by sign-up.
                options.SignIn.RequireConfirmedAccount = false;

                options.Password.RequiredLength         = 8;
                options.Password.RequireDigit           = true;
                options.Password.RequireUppercase       = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers      = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}
