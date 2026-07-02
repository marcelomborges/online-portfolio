using Microsoft.EntityFrameworkCore;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Data.Repositories;

public sealed class UserRepository(ApplicationDbContext db) : IUserRepository
{
    public Task<ApplicationUser?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<ApplicationUser?> FindByIdWithTenantAsync(Guid id, CancellationToken ct = default) =>
        db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id, ct);
}
