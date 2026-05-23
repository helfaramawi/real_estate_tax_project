using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Risk;

[Collection("Integration")]
public class RiskEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public RiskEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetRiskScore_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync($"/api/risk/property/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task GetRiskScore_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync($"/api/risk/property/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Recalculate_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsync($"/api/risk/recalculate/{Guid.NewGuid()}", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Recalculate_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsync($"/api/risk/recalculate/{Guid.NewGuid()}", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetFraudFlags_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync("/api/fraud-flags");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFraudFlags_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync("/api/fraud-flags");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }



    [Fact]
    public async Task CreateFraudFlag_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();

        var response = await unauthenticated.PostAsJsonAsync("/api/fraud-flags", new
        {
            propertyId = Guid.NewGuid(),
            flagType = 0,
            reason = "unauthenticated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFraudFlag_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync("/api/fraud-flags", new
        {
            propertyId = Guid.NewGuid(),
            flagType = 0,
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateFraudFlag_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync("/api/fraud-flags", new
        {
            propertyId = Guid.NewGuid(),
            flagType = 0,
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}
