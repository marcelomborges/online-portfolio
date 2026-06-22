using Microsoft.AspNetCore.Mvc;
using OnlinePortfolio.Api.Controllers;

namespace OnlinePortfolio.Api.Tests.Unit;

public sealed class HealthControllerTests
{
    [Fact]
    public void Ping_ReturnsOkWithServiceStatus()
    {
        var controller = new HealthController();

        var result = controller.Ping();

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        ok.Value.Should().BeEquivalentTo(new
        {
            service = "OnlinePortfolio.Api",
            status = "ok",
        });
    }
}
