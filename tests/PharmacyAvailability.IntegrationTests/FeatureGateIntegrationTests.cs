using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class FeatureGateIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FeatureGateIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReservationsEndpoint_WhenDisabled_Returns503ServiceUnavailable()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/reservations");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Feature Disabled", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AvailabilityEndpoint_RemainsActive_Returns200OK()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/availability?q=Panadol");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ActiveBranchesEndpoint_RemainsActive_Returns200OK()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/branches");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ActivePharmaciesEndpoint_RemainsActive_Returns200OK()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/pharmacies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
