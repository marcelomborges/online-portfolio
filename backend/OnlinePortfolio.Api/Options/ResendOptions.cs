namespace OnlinePortfolio.Api.Options;

public sealed class ResendOptions
{
    public const string Section = "Resend";

    public string ApiKey { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
}
