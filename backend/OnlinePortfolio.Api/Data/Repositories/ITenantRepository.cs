using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Repositories;

public interface ITenantRepository
{
    Task<Tenant?> FindByIdAsync(Guid id, CancellationToken ct = default);
}
