using OnlinePortfolio.Api.Common;
using OnlinePortfolio.Api.Models.Platform;

namespace OnlinePortfolio.Api.Services;

public interface IPlatformService
{
    Task<Result<IReadOnlyList<TenantUserResponse>>> GetTenantUsersAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<TenantUserResponse>> InviteUserAsync(Guid tenantId, InviteUserRequest request, Guid invitedByUserId, CancellationToken ct = default);
    Task<Result> UpdateUserAsync(Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default);
}
