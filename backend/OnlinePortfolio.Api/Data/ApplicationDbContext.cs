using Microsoft.EntityFrameworkCore;

namespace OnlinePortfolio.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // DbSets added in DEV-150+ (Plan, Tenant, ApplicationUser, …)
}
