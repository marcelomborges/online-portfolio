namespace OnlinePortfolio.Api.Models.Auth;

public sealed record MeResponse(
    Guid Id,
    string Email,
    string Role,
    TenantInfo? Tenant);

public sealed record TenantInfo(
    Guid Id,
    string Slug,
    string DisplayName);
