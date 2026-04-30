using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RealEstateTax.Application.DTOs.Auth;

namespace RealEstateTax.IntegrationTests.Auth;

[Collection("Integration")]
public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.InitialiseDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ── POST /api/auth/login ───────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = CustomWebApplicationFactory.AdminUsername, password = CustomWebApplicationFactory.AdminPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<LoginSuccessEnvelope>();
        body.Should().NotBeNull();
        body!.Data.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns400WithErrorMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = CustomWebApplicationFactory.AdminUsername, password = "WrongPass!99" });

        // AuthAppService returns Result.Failure (no explicit error code) → ApiResponseExtensions maps to 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task Login_UnknownUser_Returns400WithErrorMessage()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "nobody", password = "Pass@1234" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task Login_EmptyUsername_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "", password = "Pass@1234" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "admin", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Protected endpoint without token ─────────────────────────────────────

    [Fact]
    public async Task GetProperties_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/properties");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Helper records (deserialize response envelope) ────────────────────────

    private record LoginSuccessEnvelope(bool IsSuccess, TokenResponse Data);
}
