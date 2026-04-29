using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Infrastructure.Persistence;

namespace RealEstateTax.IntegrationTests;

/// <summary>
/// Replaces PostgreSQL + Hangfire.PostgreSql with in-memory providers so tests
/// run without an external database. Each factory instance gets its own isolated
/// in-memory database (unique DB name per instance).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IntegrationTest_{Guid.NewGuid()}";

    public const string AdminUsername = "testadmin";
    public const string AdminPassword = "Admin@12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"]   = "IntegrationTestSecretKey_MustBe32CharsMin!!",
                ["Jwt:Issuer"]   = "RealEstateTaxTestIssuer",
                ["Jwt:Audience"] = "RealEstateTaxTestAudience",
                // Unused by tests (EF Core in-memory replaces the real DB)
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=integration_tests_unused"
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── EF Core: replace PostgreSQL with in-memory ─────────────────────
            var efDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (efDescriptor != null) services.Remove(efDescriptor);

            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // ── Hangfire: replace PostgreSQL storage with in-memory ────────────
            // Remove all Hangfire service registrations from the Infrastructure DI
            var hangfireDescriptors = services
                .Where(d => d.ServiceType.Namespace?.StartsWith("Hangfire") == true ||
                            d.ImplementationType?.Namespace?.StartsWith("Hangfire") == true)
                .ToList();
            foreach (var d in hangfireDescriptors) services.Remove(d);

            services.AddHangfire(c => c.UseInMemoryStorage());
            // Do NOT add HangfireServer — avoids background processing during tests

            // ── Remove NpgSql health check (no real DB) ───────────────────────
            var hcDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("NpgSql", StringComparison.OrdinalIgnoreCase) == true ||
                            d.ImplementationType?.FullName?.Contains("NpgSql", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            foreach (var d in hcDescriptors) services.Remove(d);
        });
    }

    /// <summary>Seed a test admin user. Call from IAsyncLifetime.InitializeAsync.</summary>
    public async Task InitialiseDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Username = AdminUsername,
                Email = "testadmin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                FirstName = "Test",
                LastName = "Admin",
                IsActive = true,
                CreatedBy = "system"
            });
            await db.SaveChangesAsync();
        }
    }
}
