using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Taxpayers;

[Collection("Integration")]
public class TaxpayersEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public TaxpayersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitialiseDatabaseAsync();
        _client = await AuthenticatedHttpClient.CreateAsync(
            _factory,
            CustomWebApplicationFactory.AdminUsername,
            CustomWebApplicationFactory.AdminPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/taxpayers ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Authenticated_Returns200()
    {
        var response = await _client.GetAsync("/api/taxpayers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync("/api/taxpayers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task GetAll_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync("/api/taxpayers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }



    [Fact]
    public async Task GetAll_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync("/api/taxpayers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.GetAsync("/api/taxpayers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.GetAsync("/api/taxpayers");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/taxpayers ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidIndividualTaxpayer_Returns201()
    {
        var request = new
        {
            nationalId = "29901011234567",   // valid structure: 1999-01-01, gov 12
            firstName = "Ahmed",
            lastName = "Hassan",
            isCorporate = false,
            phoneNumber = "01012345678",
            governorate = "Cairo",
            city = "Cairo"
        };

        var response = await _client.PostAsJsonAsync("/api/taxpayers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedEnvelope>();
        body!.Data.Id.Should().NotBeEmpty();
        body.Data.TaxpayerCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_InvalidNationalId_Returns400()
    {
        var request = new
        {
            nationalId = "19901011234567",  // century digit=1 → invalid
            firstName = "Test",
            lastName = "User",
            isCorporate = false
        };

        var response = await _client.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400()
    {
        var request = new
        {
            nationalId = "29901011234567"
            // missing firstName, lastName
        };

        var response = await _client.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }




    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();

        var request = new
        {
            nationalId = "29901011234567",
            firstName = "Anon",
            lastName = "User",
            isCorporate = false
        };

        var response = await unauthenticated.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var request = new
        {
            nationalId = "29901011234567",
            firstName = "Citizen",
            lastName = "User",
            isCorporate = false
        };

        var response = await citizenClient.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task Create_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var request = new
        {
            nationalId = "29901011234567",
            firstName = "Field",
            lastName = "Inspector",
            isCorporate = false
        };

        var response = await inspectorClient.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var request = new
        {
            nationalId = "29901011234567",
            firstName = "Tax",
            lastName = "Officer",
            isCorporate = false
        };

        var response = await officerClient.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var request = new
        {
            nationalId = "29901011234567",
            firstName = "Tax",
            lastName = "Assessor",
            isCorporate = false
        };

        var response = await assessorClient.PostAsJsonAsync("/api/taxpayers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/taxpayers/{id} ───────────────────────────────────────────────



    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync($"/api/taxpayers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }



    [Fact]
    public async Task GetById_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync($"/api/taxpayers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync($"/api/taxpayers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.GetAsync($"/api/taxpayers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.GetAsync($"/api/taxpayers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/taxpayers/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helper records ────────────────────────────────────────────────────────

    private record TaxpayerSummary(Guid Id, string TaxpayerCode);
    private record CreatedEnvelope(bool Success, TaxpayerSummary Data);
}
