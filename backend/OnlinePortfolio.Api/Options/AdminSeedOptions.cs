namespace OnlinePortfolio.Api.Options;

public sealed class AdminSeedOptions
{
    public const string Section = "AdminSeed";

    /// <summary>Email of the PlatformAdmin to create on first startup.</summary>
    /// <remarks>Set AdminSeed__Email + AdminSeed__Password as Render env vars to bootstrap prod.</remarks>
    public string? Email { get; init; }
    public string? Password { get; init; }
}
