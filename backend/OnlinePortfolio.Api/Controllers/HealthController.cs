using Microsoft.AspNetCore.Mvc;

namespace OnlinePortfolio.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping() =>
        Ok(new { service = "OnlinePortfolio.Api", status = "ok" });
}
