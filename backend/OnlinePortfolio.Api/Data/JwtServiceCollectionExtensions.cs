using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OnlinePortfolio.Api.Options;

namespace OnlinePortfolio.Api.Data;

public static class JwtServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationJwt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(opts.Secret))
            throw new InvalidOperationException("Jwt:Secret is required.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Secret));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = true,
                    ValidIssuer              = opts.Issuer,
                    ValidateAudience         = true,
                    ValidAudience            = opts.Audience,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.FromSeconds(30),
                };
            });

        return services;
    }
}
