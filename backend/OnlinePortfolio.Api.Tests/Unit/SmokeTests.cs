namespace OnlinePortfolio.Api.Tests.Unit;

public sealed class SmokeTests
{
    [Fact]
    public void TestProject_ShouldReferenceApiAssembly()
    {
        typeof(OnlinePortfolio.Api.Controllers.HealthController).Assembly.GetName().Name
            .Should()
            .Be("OnlinePortfolio.Api");
    }
}
