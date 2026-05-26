using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Exemptions;

[Collection("Integration")]
public class ExemptionsEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public ExemptionsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetByProperty_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync($"/api/exemptions/property/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task GetByProperty_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync($"/api/exemptions/property/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetByProperty_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync($"/api/exemptions/property/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Submit_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync("/api/exemptions", new
        {
            propertyId = Guid.NewGuid(),
            exemptionType = 0,
            reason = "test"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task Approve_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync($"/api/exemptions/{Guid.NewGuid()}/approve", new
        {
            reviewerNotes = "unauthenticated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Approve_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync($"/api/exemptions/{Guid.NewGuid()}/approve", new
        {
            reviewerNotes = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }




    [Fact]
    public async Task Approve_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync($"/api/exemptions/{Guid.NewGuid()}/approve", new
        {
            reviewerNotes = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reject_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync($"/api/exemptions/{Guid.NewGuid()}/reject", new
        {
            reason = "unauthenticated"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reject_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync($"/api/exemptions/{Guid.NewGuid()}/reject", new
        {
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reject_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync($"/api/exemptions/{Guid.NewGuid()}/reject", new
        {
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}
