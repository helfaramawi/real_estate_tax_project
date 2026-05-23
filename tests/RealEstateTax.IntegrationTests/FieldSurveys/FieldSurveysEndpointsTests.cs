using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.FieldSurveys;

[Collection("Integration")]
public class FieldSurveysEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public FieldSurveysEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync("/api/field-surveys");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }




    [Fact]
    public async Task GetAll_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync("/api/field-surveys");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }



    [Fact]
    public async Task GetAll_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync("/api/field-surveys");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync("/api/field-surveys", new
        {
            propertyId = Guid.NewGuid(),
            assignedToUserId = Guid.NewGuid(),
            notes = "unauthenticated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync("/api/field-surveys", new
        {
            propertyId = Guid.NewGuid(),
            assignedToUserId = Guid.NewGuid(),
            notes = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }



    [Fact]
    public async Task Create_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync("/api/field-surveys", new
        {
            propertyId = Guid.NewGuid(),
            assignedToUserId = Guid.NewGuid(),
            notes = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Submit_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsync($"/api/field-surveys/{Guid.NewGuid()}/submit", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Submit_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsync($"/api/field-surveys/{Guid.NewGuid()}/submit", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Submit_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsync($"/api/field-surveys/{Guid.NewGuid()}/submit", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}
