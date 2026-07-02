using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnlinePortfolio.Api.Common;
using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Data.Entities;
using OnlinePortfolio.Api.Data.Repositories;
using OnlinePortfolio.Api.Models.Auth;
using OnlinePortfolio.Api.Options;

namespace OnlinePortfolio.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IUserRepository users,
    IUnitOfWork uow,
    IOptions<JwtOptions> jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // Use repository to eager-load Tenant in one query.
        var userByEmail = await userManager.FindByEmailAsync(request.Email);

        // Generic message — do not reveal whether the email exists.
        if (userByEmail is null)
            return Result<TokenResponse>.Fail("Auth.InvalidCredentials", "Invalid email or password.");

        var user = await users.FindByIdWithTenantAsync(userByEmail.Id, ct)
                   ?? userByEmail;

        // Return the same error as wrong password to prevent user/tenant enumeration.
        if (!user.IsActive)
            return Result<TokenResponse>.Fail("Auth.InvalidCredentials", "Invalid email or password.");

        if (user.TenantId is not null && user.Tenant is not null && !user.Tenant.IsActive)
            return Result<TokenResponse>.Fail("Auth.InvalidCredentials", "Invalid email or password.");

        var signIn = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            logger.LogWarning("Login blocked — account locked out {Email}", user.Email);
            return Result<TokenResponse>.Fail("Auth.LockedOut", "Account is locked. Try again later.");
        }

        if (!signIn.Succeeded)
        {
            logger.LogWarning("Failed login attempt for {Email}", user.Email);
            return Result<TokenResponse>.Fail("Auth.InvalidCredentials", "Invalid email or password.");
        }

        var roles = await userManager.GetRolesAsync(user);

        var token = BuildToken(user, roles);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await uow.CommitAsync(ct);

        logger.LogInformation("User {Email} logged in", user.Email);

        return Result<TokenResponse>.Ok(token);
    }

    public async Task<Result> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        // Generic message — do not reveal whether the email exists.
        if (user is null)
            return Result.Fail(Error.Validation("Invalid or expired invite."));

        // Reject if the invite was already accepted. Prevents using a stale invite token
        // to reset the password of an already-active account.
        if (user.EmailConfirmed)
            return Result.Fail(Error.Validation("Invalid or expired invite."));

        var resetResult = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!resetResult.Succeeded)
            return Result.Fail(Error.Validation("Invalid or expired invite."));

        user.EmailConfirmed = true;
        user.IsActive = true;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            logger.LogWarning("AcceptInvite: failed to update user {Email}: {Errors}", request.Email, errors);
            return Result.Fail(Error.Validation("Could not activate account."));
        }

        logger.LogInformation("User {Email} accepted invite", user.Email);
        return Result.Ok();
    }

    public async Task<Result<MeResponse>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdWithTenantAsync(userId, ct);

        if (user is null)
            return Result<MeResponse>.Fail(Error.Unauthorized());

        if (!user.IsActive)
            return Result<MeResponse>.Fail(Error.Forbidden());

        var roles = await userManager.GetRolesAsync(user);
        var role  = roles.FirstOrDefault() ?? string.Empty;

        TenantInfo? tenantInfo = null;
        if (user.Tenant is not null)
        {
            if (!user.Tenant.IsActive)
                return Result<MeResponse>.Fail(Error.Forbidden());

            tenantInfo = new TenantInfo(user.Tenant.Id, user.Tenant.Slug, user.Tenant.DisplayName);
        }

        return Result<MeResponse>.Ok(new MeResponse(user.Id, user.Email!, role, tenantInfo));
    }

    private TokenResponse BuildToken(ApplicationUser user, IList<string> roles)
    {
        var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds     = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (user.TenantId.HasValue)
            claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));

        var jwt = new JwtSecurityToken(
            issuer:             _jwt.Issuer,
            audience:           _jwt.Audience,
            claims:             claims,
            expires:            expiresAt.UtcDateTime,
            signingCredentials: creds);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new TokenResponse(
            AccessToken: token,
            TokenType:   "Bearer",
            ExpiresIn:   _jwt.ExpirationMinutes * 60,
            ExpiresAt:   expiresAt);
    }
}
