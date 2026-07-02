using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace OnlinePortfolio.Api.Infrastructure;

public static class AuthRateLimitPolicies
{
    public const string Login        = "auth:login";
    public const string AcceptInvite = "auth:invite";
}

public static class ApplicationRateLimiterExtensions
{

    public static IServiceCollection AddApplicationRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Login: 10 attempts per minute per IP.
            // Identity lockout (5 attempts / 15 min per user) is a second independent layer.
            options.AddPolicy(AuthRateLimitPolicies.Login, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit          = 10,
                    Window               = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit           = 0,
                });
            });

            // Accept-invite: 5 attempts per minute per IP.
            options.AddPolicy(AuthRateLimitPolicies.AcceptInvite, context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"invite:{ip}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit          = 5,
                    Window               = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit           = 0,
                });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
