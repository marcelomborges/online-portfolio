using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlinePortfolio.Api.Data;
using OnlinePortfolio.Api.Models.Platform;
using OnlinePortfolio.Api.Services;

namespace OnlinePortfolio.Api.Controllers;

[ApiController]
[Route("api/v1/platform")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.PlatformAdmin)]
public sealed class PlatformController(IPlatformService platform) : ControllerBase
{
    [HttpGet("tenants/{tenantId:guid}/users")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsers(Guid tenantId, CancellationToken ct)
    {
        var result = await platform.GetTenantUsersAsync(tenantId, ct);
        return result.Match<IActionResult>(
            onSuccess: users => Ok(users),
            onFailure: err   => err.Code.EndsWith(".NotFound") ? NotFound() : BadRequest(new { error = err.Message }));
    }

    [HttpPost("tenants/{tenantId:guid}/users/invite")]
    [ProducesResponseType(typeof(TenantUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteUser(
        Guid tenantId,
        [FromBody] InviteUserRequest request,
        CancellationToken ct)
    {
        var invitedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(invitedByUserId, out var adminId))
            return Unauthorized();

        var result = await platform.InviteUserAsync(tenantId, request, adminId, ct);
        return result.Match<IActionResult>(
            onSuccess: user => CreatedAtAction(
                nameof(GetUsers),
                new { tenantId },
                user),
            onFailure: err => err.Code switch
            {
                "User.Conflict"   => Conflict(new { error = err.Message }),
                "Tenant.NotFound" => NotFound(),
                _                 => BadRequest(new { error = err.Message }),
            });
    }

    [HttpPatch("tenants/{tenantId:guid}/users/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        Guid tenantId,
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var result = await platform.UpdateUserAsync(tenantId, userId, request, ct);
        return result.Match<IActionResult>(
            onSuccess: () => NoContent(),
            onFailure: err => err.Code switch
            {
                "Tenant.NotFound" or "User.NotFound" => NotFound(),
                _                                    => BadRequest(new { error = err.Message }),
            });
    }
}
