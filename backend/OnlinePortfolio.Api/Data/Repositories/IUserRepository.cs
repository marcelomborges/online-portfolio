using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ApplicationUser?> FindByIdWithTenantAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationUser>> ListByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByEmailAndTenantAsync(string normalizedEmail, Guid tenantId, CancellationToken ct = default);
}
