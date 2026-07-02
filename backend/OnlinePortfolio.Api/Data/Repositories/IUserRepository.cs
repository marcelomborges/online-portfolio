using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationUser?> FindByIdWithTenantAsync(Guid id, CancellationToken ct = default);
}
