using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Data.Entities;

namespace OnlinePortfolio.Api.Tests.Unit;

public sealed class DevDataSeederTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<UserManager<ApplicationUser>> _userManager;

    private const string AdminEmail    = "admin@onlineportfolio.com.br";
    private const string AdminPassword = "Dev@12345";

    public DevDataSeederTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _userManager = CreateUserManagerMock();
    }

    [Fact]
    public async Task SeedAsync_CreatesPlanTenantsAndAdmin_WhenDatabaseIsEmpty()
    {
        await RunSeedAsync();

        // Plan
        var plan = await _db.Plans.SingleAsync();
        plan.Name.Should().Be("Starter");

        // Tenants
        var tenants = await _db.Tenants.OrderBy(t => t.Slug).ToListAsync();
        tenants.Should().HaveCount(2);
        tenants.Select(t => t.Slug).Should().BeEquivalentTo(["ana", "joao"]);

        // TenantSettings created for both
        var settings = await _db.TenantSettings.ToListAsync();
        settings.Should().HaveCount(2);

        // PlatformAdmin created once
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), AdminPassword), Times.Once);
        _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), AppRoles.PlatformAdmin), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent_WhenRunTwice()
    {
        await RunSeedAsync();
        await RunSeedAsync();

        (await _db.Plans.CountAsync()).Should().Be(1);
        (await _db.Tenants.CountAsync()).Should().Be(2);
        (await _db.TenantSettings.CountAsync()).Should().Be(2);

        // User lookup returns existing on second run — CreateAsync called only once
        _userManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), AdminPassword), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_AssignsPlatformAdminRole()
    {
        await RunSeedAsync();

        _userManager.Verify(
            m => m.AddToRoleAsync(
                It.Is<ApplicationUser>(u => u.Email == AdminEmail),
                AppRoles.PlatformAdmin),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_TenantSettingsMatchTenantIds()
    {
        await RunSeedAsync();

        var tenantIds = await _db.Tenants.Select(t => t.Id).ToListAsync();
        var settingsIds = await _db.TenantSettings.Select(s => s.TenantId).ToListAsync();

        settingsIds.Should().BeEquivalentTo(tenantIds);
    }

    // --- helpers ---

    private Task RunSeedAsync() =>
        DevDataSeeder.SeedAsync(_db, _userManager.Object, AdminEmail, AdminPassword,
            NullLoggerFactory.Instance.CreateLogger("test"));

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var manager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        ApplicationUser? createdUser = null;

        manager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(() => createdUser);

        manager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser user, string _) =>
            {
                createdUser = user;
                return IdentityResult.Success;
            });

        manager
            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        return manager;
    }

    public void Dispose() => _db.Dispose();
}
