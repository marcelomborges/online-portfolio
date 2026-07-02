namespace OnlinePortfolio.Api.Options;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Secret { get; init; } = string.Empty;
    public string Issuer { get; init; } = "OnlinePortfolio.Api";
    public string Audience { get; init; } = "OnlinePortfolio.Admin";
    public int ExpirationMinutes { get; init; } = 60;
}
