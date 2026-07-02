using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Data.Entities;
using OnlinePortfolio.Api.Data.Repositories;
using OnlinePortfolio.Api.Models.Auth;
using MsOptions = Microsoft.Extensions.Options;
using OnlinePortfolio.Api.Options;
using OnlinePortfolio.Api.Services;

namespace OnlinePortfolio.Api.Tests.Unit;

public sealed class AuthServiceTests
{
    private const string ValidSecret = "test-secret-that-is-long-enough-for-hmac!!";

    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManager;
    private readonly Mock<IUserRepository> _users;
    private readonly Mock<IUnitOfWork> _uow;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory   = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManager = new Mock<SignInManager<ApplicationUser>>(
            _userManager.Object, contextAccessor.Object, claimsFactory.Object,
            null!, null!, null!, null!);

        _users = new Mock<IUserRepository>();
        _uow   = new Mock<IUnitOfWork>();
    }

    private IAuthService Build() => new AuthService(
        _userManager.Object,
        _signInManager.Object,
        _users.Object,
        _uow.Object,
        MsOptions.Options.Create(new JwtOptions { Secret = ValidSecret, ExpirationMinutes = 60 }),
        NullLogger<AuthService>.Instance);

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsValid()
    {
        var user = ActiveUser();
        SetupLoginSuccess(user);

        var result = await Build().LoginAsync(new LoginRequest(user.Email!, "Correct@1"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_ReturnsFailure_WhenUserNotFound()
    {
        _userManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await Build().LoginAsync(new LoginRequest("nobody@x.com", "pw"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Login_ReturnsInvalidCredentials_WhenUserInactive()
    {
        // Inactive user returns same error as wrong password to prevent user enumeration.
        var user = ActiveUser();
        user.IsActive = false;
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _users.Setup(r => r.FindByIdWithTenantAsync(user.Id, default)).ReturnsAsync(user);

        var result = await Build().LoginAsync(new LoginRequest(user.Email!, "pw"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Login_ReturnsLockedOut_WhenAccountLocked()
    {
        var user = ActiveUser();
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _users.Setup(r => r.FindByIdWithTenantAsync(user.Id, default)).ReturnsAsync(user);
        _signInManager
            .Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.LockedOut);

        var result = await Build().LoginAsync(new LoginRequest(user.Email!, "wrong"), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.LockedOut");
    }

    [Fact]
    public async Task Login_UpdatesLastLoginAt_OnSuccess()
    {
        var user = ActiveUser();
        SetupLoginSuccess(user);
        _uow.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);

        await Build().LoginAsync(new LoginRequest(user.Email!, "Correct@1"), default);

        user.LastLoginAt.Should().NotBeNull();
        _uow.Verify(u => u.CommitAsync(default), Times.Once);
    }

    // ── GetCurrentUser ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUser_ReturnsMeResponse_ForPlatformAdmin()
    {
        var user = ActiveUser(tenantId: null);
        _users.Setup(r => r.FindByIdWithTenantAsync(user.Id, default)).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["PlatformAdmin"]);

        var result = await Build().GetCurrentUserAsync(user.Id, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("PlatformAdmin");
        result.Value.Tenant.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsUnauthorized_WhenUserNotFound()
    {
        _users.Setup(r => r.FindByIdWithTenantAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await Build().GetCurrentUserAsync(Guid.NewGuid(), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Unauthorized");
    }

    // ── AcceptInvite ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptInvite_ReturnsOk_WhenTokenValid()
    {
        var user = ActiveUser();
        user.IsActive     = false;
        user.EmailConfirmed = false;
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager
            .Setup(m => m.ResetPasswordAsync(user, "valid-token", "New@12345"))
            .ReturnsAsync(IdentityResult.Success);
        _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await Build().AcceptInviteAsync(
            new AcceptInviteRequest(user.Email!, "valid-token", "New@12345"), default);

        result.IsSuccess.Should().BeTrue();
        user.EmailConfirmed.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AcceptInvite_ReturnsFailure_WhenAccountAlreadyActive()
    {
        // Prevents using a stale invite token to reset a live account's password.
        var user = ActiveUser();
        user.EmailConfirmed = true;
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var result = await Build().AcceptInviteAsync(
            new AcceptInviteRequest(user.Email!, "any-token", "New@12345"), default);

        result.IsFailure.Should().BeTrue();
        _userManager.Verify(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AcceptInvite_ReturnsFailure_WhenTokenInvalid()
    {
        var user = ActiveUser();
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManager
            .Setup(m => m.ResetPasswordAsync(user, "bad-token", It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await Build().AcceptInviteAsync(
            new AcceptInviteRequest(user.Email!, "bad-token", "New@12345"), default);

        result.IsFailure.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ApplicationUser ActiveUser(Guid? tenantId = null) => new()
    {
        Id              = Guid.NewGuid(),
        Email           = "user@example.com",
        UserName        = "user@example.com",
        IsActive        = true,
        EmailConfirmed  = true,
        TenantId        = tenantId,
        CreatedAt       = DateTimeOffset.UtcNow,
    };

    private void SetupLoginSuccess(ApplicationUser user)
    {
        _userManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _users.Setup(r => r.FindByIdWithTenantAsync(user.Id, default)).ReturnsAsync(user);
        _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["PlatformAdmin"]);
        _signInManager
            .Setup(s => s.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(SignInResult.Success);
        _uow.Setup(u => u.CommitAsync(default)).ReturnsAsync(1);
    }
}
