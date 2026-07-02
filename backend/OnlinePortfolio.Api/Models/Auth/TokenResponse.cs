namespace OnlinePortfolio.Api.Models.Auth;

public sealed record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset ExpiresAt);
