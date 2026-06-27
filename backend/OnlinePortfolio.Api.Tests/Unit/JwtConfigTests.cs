using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlinePortfolio.Api.Data;

namespace OnlinePortfolio.Api.Tests.Unit;

public sealed class JwtConfigTests
{
    [Fact]
    public void AddApplicationJwt_ThrowsWhenSecretMissing()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        var act = () => services.AddApplicationJwt(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void AddApplicationJwt_ThrowsWhenSecretIsWhitespace()
    {
        var config = BuildConfig(secret: "   ");
        var services = new ServiceCollection();

        var act = () => services.AddApplicationJwt(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Secret*");
    }

    [Fact]
    public void AddApplicationJwt_RegistersAuthenticationWithValidSecret()
    {
        var config = BuildConfig(secret: "valid-secret-long-enough-for-hmac!!");
        var services = new ServiceCollection();

        var act = () => services.AddApplicationJwt(config);

        act.Should().NotThrow();
    }

    private static IConfiguration BuildConfig(string secret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = secret,
                ["Jwt:Issuer"]   = "OnlinePortfolio.Api",
                ["Jwt:Audience"] = "OnlinePortfolio.Admin",
            })
            .Build();
}
