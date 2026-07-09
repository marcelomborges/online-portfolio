namespace OnlinePortfolio.Api.Models.Platform;

public sealed record UpdateUserRequest(
    bool? IsActive,
    string? Role);
