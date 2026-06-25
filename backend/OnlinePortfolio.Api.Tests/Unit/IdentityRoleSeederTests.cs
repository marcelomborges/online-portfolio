using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OnlinePortfolio.Api.Data;

namespace OnlinePortfolio.Api.Tests.Unit;

public sealed class IdentityRoleSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesEachRoleWhenMissing()
    {
        var existing = new HashSet<string>(StringComparer.Ordinal);
        var roleManager = CreateRoleManager(existing);

        await IdentityRoleSeeder.SeedAsync(roleManager.Object, NullLoggerFactory.Instance.CreateLogger("test"));

        existing.Should().BeEquivalentTo(AppRoles.All);
        roleManager.Verify(
            m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()),
            Times.Exactly(AppRoles.All.Count));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotentWhenRolesExist()
    {
        var existing = new HashSet<string>(AppRoles.All, StringComparer.Ordinal);
        var roleManager = CreateRoleManager(existing);

        await IdentityRoleSeeder.SeedAsync(roleManager.Object, NullLoggerFactory.Instance.CreateLogger("test"));

        roleManager.Verify(
            m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()),
            Times.Never);
    }

    private static Mock<RoleManager<IdentityRole<Guid>>> CreateRoleManager(ISet<string> existingRoles)
    {
        var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
        var manager = new Mock<RoleManager<IdentityRole<Guid>>>(
            store.Object,
            null!,
            null!,
            null!,
            null!);

        manager
            .Setup(m => m.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string role) => existingRoles.Contains(role));

        manager
            .Setup(m => m.CreateAsync(It.IsAny<IdentityRole<Guid>>()))
            .ReturnsAsync((IdentityRole<Guid> role) =>
            {
                existingRoles.Add(role.Name!);
                return IdentityResult.Success;
            });

        return manager;
    }
}
