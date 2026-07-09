using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OnlinePortfolio.Api.Common;
using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Data.Entities;
using OnlinePortfolio.Api.Data.Repositories;
using OnlinePortfolio.Api.Models.Platform;
using OnlinePortfolio.Api.Options;

namespace OnlinePortfolio.Api.Services;

public sealed class PlatformService(
    UserManager<ApplicationUser> userManager,
    IUserRepository users,
    ITenantRepository tenants,
    IEmailService email,
    IOptions<AppOptions> appOpts,
    ILogger<PlatformService> logger) : IPlatformService
{
    private readonly string _appBaseUrl = appOpts.Value.BaseUrl;

    // Valid roles for tenant users — PlatformAdmin is not assignable via this endpoint.
    private static readonly IReadOnlySet<string> AllowedTenantRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { AppRoles.Owner, AppRoles.Editor };

    public async Task<Result<IReadOnlyList<TenantUserResponse>>> GetTenantUsersAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await tenants.FindByIdAsync(tenantId, ct);
        if (tenant is null)
            return Result<IReadOnlyList<TenantUserResponse>>.Fail(Error.NotFound("Tenant"));

        var tenantUsers = await users.ListByTenantAsync(tenantId, ct);

        var result = new List<TenantUserResponse>(tenantUsers.Count);
        foreach (var user in tenantUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new TenantUserResponse(
                user.Id,
                user.Email!,
                roles.FirstOrDefault() ?? string.Empty,
                user.IsActive,
                user.LastLoginAt,
                user.CreatedAt));
        }

        return Result<IReadOnlyList<TenantUserResponse>>.Ok(result);
    }

    public async Task<Result<TenantUserResponse>> InviteUserAsync(
        Guid tenantId, InviteUserRequest request, Guid invitedByUserId, CancellationToken ct = default)
    {
        if (!AllowedTenantRoles.Contains(request.Role))
            return Result<TenantUserResponse>.Fail(Error.Validation($"Role must be '{AppRoles.Owner}' or '{AppRoles.Editor}'."));

        var tenant = await tenants.FindByIdAsync(tenantId, ct);
        if (tenant is null)
            return Result<TenantUserResponse>.Fail(Error.NotFound("Tenant"));

        // Reject duplicate email anywhere in the system to surface a meaningful error
        // instead of letting UserManager.CreateAsync fail with an opaque identity error.
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Result<TenantUserResponse>.Fail(Error.Conflict("User"));

        var user = new ApplicationUser
        {
            UserName         = request.Email,
            Email            = request.Email,
            TenantId         = tenantId,
            IsActive         = false,   // activated on accept-invite
            EmailConfirmed   = false,   // confirmed on accept-invite
            InvitedByUserId  = invitedByUserId,
            CreatedAt        = DateTimeOffset.UtcNow,
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            logger.LogWarning("InviteUser: failed to create user {Email}: {Errors}", request.Email, errors);
            return Result<TenantUserResponse>.Fail(Error.Validation("Could not create user."));
        }

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Result<TenantUserResponse>.Fail(Error.Validation("Could not assign role."));
        }

        var token        = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var encodedEmail = Uri.EscapeDataString(request.Email);
        var inviteLink   = $"{_appBaseUrl}/accept-invite?email={encodedEmail}&token={encodedToken}";

        try
        {
            await email.SendInviteAsync(request.Email, tenant.DisplayName, inviteLink, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InviteUser: failed to send invite email to {Email}", request.Email);
            // Log the link at Debug so local dev can copy it without the email. Never shown in prod (min level = Information).
            logger.LogDebug("InviteUser: invite link for {Email} — {Link}", request.Email, inviteLink);
        }

        logger.LogInformation("User {Email} invited to tenant {TenantId} as {Role}", request.Email, tenantId, request.Role);

        return Result<TenantUserResponse>.Ok(new TenantUserResponse(
            user.Id, user.Email!, request.Role, user.IsActive, user.LastLoginAt, user.CreatedAt));
    }

    public async Task<Result> UpdateUserAsync(
        Guid tenantId, Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var tenant = await tenants.FindByIdAsync(tenantId, ct);
        if (tenant is null)
            return Result.Fail(Error.NotFound("Tenant"));

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.TenantId != tenantId)
            return Result.Fail(Error.NotFound("User"));

        var currentRoles = await userManager.GetRolesAsync(user);
        var currentRole  = currentRoles.FirstOrDefault() ?? string.Empty;

        if (request.Role is not null && !AllowedTenantRoles.Contains(request.Role))
            return Result.Fail(Error.Validation($"Role must be '{AppRoles.Owner}' or '{AppRoles.Editor}'."));

        // Guard: cannot remove the last active Owner of a tenant.
        bool willLoseOwnerRole = currentRole == AppRoles.Owner
            && (request.IsActive == false
                || (request.Role is not null && !string.Equals(request.Role, AppRoles.Owner, StringComparison.OrdinalIgnoreCase)));

        if (willLoseOwnerRole)
        {
            var allOwners      = await userManager.GetUsersInRoleAsync(AppRoles.Owner);
            int remainingOwners = allOwners.Count(u => u.TenantId == tenantId && u.IsActive && u.Id != userId);
            if (remainingOwners == 0)
                return Result.Fail(Error.Validation("Cannot deactivate or demote the last active Owner of a tenant."));
        }

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.Role is not null && !string.Equals(request.Role, currentRole, StringComparison.OrdinalIgnoreCase))
        {
            if (currentRoles.Count > 0)
                await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, request.Role);
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Fail(Error.Validation("Could not update user."));

        logger.LogInformation("User {UserId} updated in tenant {TenantId}: isActive={IsActive}, role={Role}",
            userId, tenantId, request.IsActive, request.Role);

        return Result.Ok();
    }
}
