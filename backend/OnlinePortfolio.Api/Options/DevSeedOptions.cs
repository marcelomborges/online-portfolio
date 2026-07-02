namespace OnlinePortfolio.Api.Options;

public sealed class DevSeedOptions
{
    public const string Section = "DevSeed";

    public string PlatformAdminEmail { get; init; } = string.Empty;
    public string PlatformAdminPassword { get; init; } = string.Empty;
}
