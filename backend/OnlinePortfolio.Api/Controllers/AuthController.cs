using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OnlinePortfolio.Api.Infrastructure;
using OnlinePortfolio.Api.Models.Auth;
using OnlinePortfolio.Api.Services;

namespace OnlinePortfolio.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimitPolicies.Login)]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var result = await auth.LoginAsync(request, ct);
        return result.Match<IActionResult>(
            onSuccess: token => Ok(token),
            onFailure: err   => err.Code switch
            {
                "Auth.Forbidden"          => StatusCode(StatusCodes.Status403Forbidden),
                "Auth.LockedOut"          => StatusCode(StatusCodes.Status423Locked),
                _                         => Unauthorized(),
            });
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        // JWT is stateless — actual cookie/token removal is handled by the Nuxt BFF (DEV-157).
        return NoContent();
    }

    [HttpPost("accept-invite")]
    [EnableRateLimiting(AuthRateLimitPolicies.AcceptInvite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteRequest request,
        CancellationToken ct)
    {
        var result = await auth.AcceptInviteAsync(request, ct);
        return result.Match<IActionResult>(
            onSuccess: ()  => NoContent(),
            onFailure: err => BadRequest(new { error = err.Message }));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userId, out var id))
            return Unauthorized();

        var result = await auth.GetCurrentUserAsync(id, ct);
        return result.Match<IActionResult>(
            onSuccess: me  => Ok(me),
            onFailure: err => err.Code switch
            {
                "Auth.Forbidden" => StatusCode(StatusCodes.Status403Forbidden),
                _                => Unauthorized(),
            });
    }
}
