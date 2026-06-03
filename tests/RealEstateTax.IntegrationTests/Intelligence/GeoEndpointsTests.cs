using System.Net;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Intelligence;

[Collection("Integration")]
public class GeoEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public GeoEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("/api/v2/geo/risk-heatmap?minLat=29.5&minLon=30.5&maxLat=30.5&maxLon=31.5")]
    [InlineData("/api/v2/geo/clusters")]
    [InlineData("/api/v2/geo/anomalies?status=Open")]
    public async Task GeoDashboardEndpoints_WithoutToken_Return401_Not404(string path)
    {
        var unauthenticated = _factory.CreateClient();

        var response = await unauthenticated.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/v2/geo/risk-heatmap?minLat=29.5&minLon=30.5&maxLat=30.5&maxLon=31.5")]
    [InlineData("/api/v2/geo/clusters")]
    [InlineData("/api/v2/geo/anomalies?status=Open")]
    public async Task GeoDashboardEndpoints_WithCitizenRole_Return403_Not404(string path)
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
