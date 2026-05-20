using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Persistence;
using RealEstateTax.IntegrationTests.Helpers;

namespace RealEstateTax.IntegrationTests.Properties;

[Collection("Integration")]
public class PropertiesEndpointsTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _creatorClient = null!;
    private HttpClient _verifierClient = null!;

    public PropertiesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitialiseDatabaseAsync();
        _creatorClient = await AuthenticatedHttpClient.CreateAsync(
            _factory,
            CustomWebApplicationFactory.AdminUsername,
            CustomWebApplicationFactory.AdminPassword);

        _verifierClient = await AuthenticatedHttpClient.CreateAsync(
            _factory,
            "superadmin",
            "Admin@12345");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpClient> CreateUserClientAsync(string username, params string[] roles)
    {
        const string password = "Admin@12345";
        roles.Should().NotBeEmpty("integration test users must have at least one role");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var normalizedRoles = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var availableRoles = await db.Roles
            .Where(r => normalizedRoles.Contains(r.Name))
            .ToListAsync();

        availableRoles.Select(r => r.Name)
            .Should()
            .BeEquivalentTo(normalizedRoles, "all requested roles must exist in seeded data");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "Integration",
                LastName = "Tester",
                IsActive = true,
                CreatedBy = "tests"
            };
            db.Users.Add(user);
        }

        var existingRoleIds = await db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        foreach (var role in availableRoles.Where(r => !existingRoleIds.Contains(r.Id)))
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedBy = "tests"
            });
        }

        await db.SaveChangesAsync();
        return await AuthenticatedHttpClient.CreateAsync(_factory, username, password);
    }

    // ── GET /api/properties ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Authenticated_Returns200()
    {
        var response = await _creatorClient.GetAsync("/api/properties");
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

        var response = await _creatorClient.PostAsJsonAsync("/api/properties", request);

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

        var response = await _creatorClient.PostAsJsonAsync("/api/properties", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async Task Verify_FromDraft_Returns200()
    {
        var createResponse = await _creatorClient.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 140.0,
            city = "Cairo",
            governorate = "Cairo"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();

        var verifyResponse = await _verifierClient.PostAsJsonAsync($"/api/properties/{created!.Data.Id}/verify", new
        {
            verificationNotes = "Wave1 verification test"
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Verify_FromVerified_Returns400()
    {
        var createResponse = await _creatorClient.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 140.0,
            city = "Cairo",
            governorate = "Cairo"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();
        var id = created!.Data.Id;

        var firstVerify = await _verifierClient.PostAsJsonAsync($"/api/properties/{id}/verify", new
        {
            verificationNotes = "First verification"
        });
        firstVerify.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondVerify = await _verifierClient.PostAsJsonAsync($"/api/properties/{id}/verify", new
        {
            verificationNotes = "Second verification should fail"
        });

        secondVerify.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/properties/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GetById_AfterCreate_Returns200()
    {
        var createResponse = await _creatorClient.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Commercial,
            builtUpArea = 200.0,
            city = "Alexandria",
            governorate = "Alexandria"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();
        var id = created!.Data.Id;

        var getResponse = await _creatorClient.GetAsync($"/api/properties/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _creatorClient.GetAsync($"/api/properties/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async Task Verify_BySameUserWhoCreatedProperty_Returns403()
    {
        var createResponse = await _creatorClient.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 110.0,
            city = "Cairo",
            governorate = "Cairo"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();

        var verifyResponse = await _creatorClient.PostAsJsonAsync($"/api/properties/{created!.Data.Id}/verify", new
        {
            verificationNotes = "Creator cannot self-verify"
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Verify_ByUserWithoutVerifierRole_Returns403()
    {
        var inspectorClient = await CreateUserClientAsync("fieldinspector1", "FieldInspector");

        var createResponse = await _creatorClient.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 112.0,
            city = "Cairo",
            governorate = "Cairo"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();

        var verifyResponse = await inspectorClient.PostAsJsonAsync($"/api/properties/{created!.Data.Id}/verify", new
        {
            verificationNotes = "Inspector should not verify"
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Verify_ByMultiRoleUser_NotCreator_Returns200()
    {
        var multiRoleVerifier = await CreateUserClientAsync("multiroleofficer", "TaxOfficer", "Citizen");

        var createResponse = await _creatorClient.PostAsJsonAsync("/api/properties", new
        {
            type = (int)PropertyType.Residential,
            builtUpArea = 115.0,
            city = "Cairo",
            governorate = "Cairo"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedEnvelope>();

        var verifyResponse = await multiRoleVerifier.PostAsJsonAsync($"/api/properties/{created!.Data.Id}/verify", new
        {
            verificationNotes = "Multi-role verifier test"
        });

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Helper records ────────────────────────────────────────────────────────

    private record PropertySummary(Guid Id, string PropertyCode);
    private record CreatedEnvelope(bool Success, PropertySummary Data);
}
