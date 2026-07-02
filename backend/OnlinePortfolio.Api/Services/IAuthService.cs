using OnlinePortfolio.Api.Common;
using OnlinePortfolio.Api.Models.Auth;

namespace OnlinePortfolio.Api.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default);
    Task<Result<MeResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
