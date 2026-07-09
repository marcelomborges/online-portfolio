using Microsoft.Extensions.DependencyInjection;

namespace OnlinePortfolio.Api.Services;

public static class ServicesServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlatformService, PlatformService>();
        services.AddTransient<IEmailService, ResendEmailService>();
        return services;
    }
}
