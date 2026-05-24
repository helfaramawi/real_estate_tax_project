using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.Domain.Enums;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Properties;

[Collection("Integration")]
public class PropertiesEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public PropertiesEndpointsTests(CustomWebApplicationFactory factory)
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

    // ── GET /api/properties ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Authenticated_Returns200()
    {
        var response = await _client.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task GetAll_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetAll_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── POST /api/properties ───────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidProperty_Returns201WithId()
    {
        var request = new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 120.5,
            streetAddress = "15 Nile Corniche",
            district = "Garden City",
            city = "Cairo",
            governorate = "Cairo",
            yearBuilt = 2010
        };

        var response = await _client.PostAsJsonAsync("/api/properties", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreatedEnvelope>();
        body!.Data.Id.Should().NotBeEmpty();
        body.Data.PropertyCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_NegativeBuiltUpArea_Returns400()
    {
        var request = new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = -1,
            city = "Cairo",
            governorate = "Cairo"
        };

        var response = await _client.PostAsJsonAsync("/api/properties", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/properties/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_AfterCreate_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Commercial,
            builtUpArea = 200.0,
            city = "Alexandria",
            governorate = "Alexandria"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();
        var id = created!.Data.Id;

        var getResponse = await _client.GetAsync($"/api/properties/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/properties/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }




    [Fact]
    public async Task GetById_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.GetAsync($"/api/properties/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.GetAsync($"/api/properties/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.GetAsync($"/api/properties/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task GetById_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.GetAsync($"/api/properties/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Verify_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();
        var response = await unauthenticated.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/verify", new
        {
            verificationNotes = "unauthenticated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Verify_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var fieldInspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await fieldInspectorClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/verify", new
        {
            verificationNotes = "role check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Verify_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/verify", new
        {
            verificationNotes = "role check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Verify_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/verify", new
        {
            verificationNotes = "role check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Verify_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/verify", new
        {
            verificationNotes = "role check"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }



    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();

        var response = await unauthenticated.DeleteAsJsonAsync($"/api/properties/{Guid.NewGuid()}", new
        {
            reason = "unauthenticated"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithTaxOfficerRole_Returns403()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 99.0,
            city = "Cairo",
            governorate = "Cairo"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();
        var propertyId = created!.Data.Id;

        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var taxOfficerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await taxOfficerClient.DeleteAsJsonAsync($"/api/properties/{propertyId}", new
        {
            reason = "not authorized role"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithTaxAssessorRole_Returns403()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 100.0,
            city = "Giza",
            governorate = "Giza"
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();
        var propertyId = created!.Data.Id;

        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.DeleteAsJsonAsync($"/api/properties/{propertyId}", new
        {
            reason = "not authorized role"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }




    [Fact]
    public async Task LinkOwner_WithoutToken_Returns401()
    {
        var unauthenticated = _factory.CreateClient();

        var response = await unauthenticated.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/link-owner", new
        {
            taxpayerId = Guid.NewGuid(),
            ownershipPercentage = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LinkOwner_WithFieldInspectorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("FieldInspector");
        var inspectorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await inspectorClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/link-owner", new
        {
            taxpayerId = Guid.NewGuid(),
            ownershipPercentage = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LinkOwner_WithCitizenRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("Citizen");
        var citizenClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await citizenClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/link-owner", new
        {
            taxpayerId = Guid.NewGuid(),
            ownershipPercentage = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LinkOwner_WithTaxOfficerRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxOfficer");
        var officerClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await officerClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/link-owner", new
        {
            taxpayerId = Guid.NewGuid(),
            ownershipPercentage = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LinkOwner_WithTaxAssessorRole_Returns403()
    {
        var creds = await _factory.CreateUserWithRoleAsync("TaxAssessor");
        var assessorClient = await AuthenticatedHttpClient.CreateAsync(_factory, creds.Username, creds.Password);

        var response = await assessorClient.PostAsJsonAsync($"/api/properties/{Guid.NewGuid()}/link-owner", new
        {
            taxpayerId = Guid.NewGuid(),
            ownershipPercentage = 50
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helper records ────────────────────────────────────────────────────────

    private record PropertySummary(Guid Id, string PropertyCode);
    private record CreatedEnvelope(bool Success, PropertySummary Data);
}
