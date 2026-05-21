using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.TaxAssessments;

[Collection("Integration")]
public class TaxAssessmentsEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public TaxAssessmentsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByProperty_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync($"/api/tax-assessments/property/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Generate_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync("/api/tax-assessments/generate", new
        {
            valuationId = Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
