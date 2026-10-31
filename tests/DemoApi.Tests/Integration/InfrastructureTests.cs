using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace DemoApi.Tests.Integration;

[Collection(ApiCollection.Name)]
public class InfrastructureTests(WebApplicationFactory<Program> factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_ReturnOk(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Metrics_AreExposed()
    {
        var client = factory.CreateClient();
        await client.GetAsync("/health/live");

        var body = await client.GetStringAsync("/metrics");

        Assert.Contains("http_requests_received_total", body);
    }

    [Fact]
    public async Task Chaos_WhenDisabled_ReturnsNotFound()
    {
        await using var disabled = factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["Chaos:Enabled"] = "false" })));

        var response = await disabled.CreateClient().GetAsync("/api/chaos/error");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
