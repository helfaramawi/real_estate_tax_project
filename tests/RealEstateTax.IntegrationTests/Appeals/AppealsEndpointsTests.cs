using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Appeals;

[Collection("Integration")]
public class AppealsEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AppealsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync("/api/appeals");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }




    [Fact]
    public async Task GetAll_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync("/api/appeals");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync("/api/appeals");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.GetAsync("/api/appeals");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.GetAsync("/api/appeals");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Submit_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync("/api/appeals", new
        {
            taxAssessmentId = Guid.NewGuid(),
            reason = "test"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync($"/api/appeals/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }




    [Fact]
    public async Task GetById_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync($"/api/appeals/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync($"/api/appeals/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assign_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();

        var response = await unauthenticated.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/assign", new
        {
            assignedToUserId = Guid.NewGuid(),
            notes = "unauthenticated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Assign_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/assign", new
        {
            assignedToUserId = Guid.NewGuid(),
            notes = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }




    [Fact]
    public async Task Assign_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/assign", new
        {
            assignedToUserId = Guid.NewGuid(),
            notes = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Decision_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/decision", new
        {
            decision = "Rejected",
            reason = "unauthenticated"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Decision_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/decision", new
        {
            decision = "Rejected",
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Decision_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/decision", new
        {
            decision = "Rejected",
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Decision_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.PostAsJsonAsync($"/api/appeals/{Guid.NewGuid()}/decision", new
        {
            decision = "Rejected",
            reason = "role guard check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

}
