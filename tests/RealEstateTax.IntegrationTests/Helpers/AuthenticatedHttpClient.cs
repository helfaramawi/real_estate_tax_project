using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RealEstateTax.IntegrationTests.Helpers;

/// <summary>Convenience wrapper that logs in and attaches the Bearer token to subsequent requests.</summary>
public static class AuthenticatedHttpClient
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static async Task<HttpClient> CreateAsync(CustomWebApplicationFactory factory, string username, string password)
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        loginResponse.EnsureSuccessStatusCode();

        var envelope = await loginResponse.Content.ReadFromJsonAsync<LoginEnvelope>(JsonOpts);
        var token = envelope?.Data?.AccessToken
            ?? throw new InvalidOperationException("Login did not return an access token.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record TokenData(string AccessToken, string RefreshToken);
    private record LoginEnvelope(bool Success, TokenData Data);
}
