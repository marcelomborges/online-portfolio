namespace OnlinePortfolio.Api.Options;

public sealed class AppOptions
{
    public const string Section = "App";

    public string BaseUrl { get; init; } = "https://app.onlineportfolio.com.br";
}
