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

    // ── Helper records ────────────────────────────────────────────────────────

    private record PropertySummary(Guid Id, string PropertyCode);
    private record CreatedEnvelope(bool Success, PropertySummary Data);
}
