using Microsoft.EntityFrameworkCore;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Repositories;

public sealed class TenantRepository(ApplicationDbContext db) : ITenantRepository
{
    public Task<Tenant?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
}
