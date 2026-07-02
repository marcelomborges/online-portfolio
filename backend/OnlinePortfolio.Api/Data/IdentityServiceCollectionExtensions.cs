using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        // AddIdentityCore instead of AddIdentity: does not register cookie auth schemes,
        // leaving JWT Bearer as the sole authentication scheme for this API.
        services
            .AddIdentityCore<ApplicationUser>(options =>
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
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        return services;
    }
}
