namespace OnlinePortfolio.Api.Models.Platform;

public sealed record TenantUserResponse(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt);
