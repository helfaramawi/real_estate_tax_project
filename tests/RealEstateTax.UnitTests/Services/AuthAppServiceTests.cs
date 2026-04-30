using FluentAssertions;
using Moq;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.DTOs.Auth;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Infrastructure.Services.ApplicationServices;
using RealEstateTax.UnitTests.Helpers;

namespace RealEstateTax.UnitTests.Services;

public class AuthAppServiceTests
{
    private readonly Mock<IApplicationDbContext> _db = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IAuditService> _audit = new();
    private readonly AuthAppService _sut;

    public AuthAppServiceTests()
    {
        _audit.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sut = new AuthAppService(_db.Object, _jwt.Object, _audit.Object);
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_UserNotFound_ReturnsFailure()
    {
        SetupUsers([]);
        var result = await _sut.LoginAsync(new LoginRequest("nobody", "pass"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task Login_WrongPassword_IncrementsFailedAttempts()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correctpassword"));
        user.FailedLoginAttempts = 0;
        SetupUsers([user]);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.LoginAsync(new LoginRequest(user.Username, "wrongpassword"), CancellationToken.None);

        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Login_FiveFailedAttempts_LocksAccount()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correctpassword"));
        user.FailedLoginAttempts = 4;
        SetupUsers([user]);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.LoginAsync(new LoginRequest(user.Username, "wrong"), CancellationToken.None);

        user.LockedUntil.Should().NotBeNull();
        user.LockedUntil.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Login_LockedAccount_ReturnsFailure_WithoutCheckingPassword()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correct"));
        user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
        SetupUsers([user]);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("locked");
    }

    [Fact]
    public async Task Login_ExpiredLock_AllowsLogin()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correct"));
        user.LockedUntil = DateTime.UtcNow.AddMinutes(-1); // expired
        user.FailedLoginAttempts = 5;
        SetupUsers([user]);
        SetupJwt(user);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsFailure()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correct"));
        user.IsActive = false;
        SetupUsers([user]);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("inactive");
    }

    [Fact]
    public async Task Login_ValidCredentials_ResetsFailedAttempts()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correct"));
        user.FailedLoginAttempts = 3;
        SetupUsers([user]);
        SetupJwt(user);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsRefreshToken()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correct"));
        SetupUsers([user]);
        SetupJwt(user);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.LoginAsync(new LoginRequest(user.Username, "correct"), CancellationToken.None);

        user.RefreshToken.Should().Be("test-refresh-token");
        user.RefreshTokenExpiry.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenResponse()
    {
        var user = ActiveUserWithPassword(BCrypt.Net.BCrypt.HashPassword("correct"));
        SetupUsers([user]);
        SetupJwt(user);
        _db.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.LoginAsync(new LoginRequest(user.Username, "correct"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("test-access-token");
        result.Data.RefreshToken.Should().Be("test-refresh-token");
        result.Data.Username.Should().Be(user.Username);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static User ActiveUserWithPassword(string passwordHash) => new()
    {
        Id = Guid.NewGuid(),
        Username = "testuser",
        Email = "test@example.com",
        PasswordHash = passwordHash,
        IsActive = true,
        FirstName = "Test",
        LastName = "User",
        UserRoles = []
    };

    private void SetupUsers(IEnumerable<User> users)
    {
        _db.Setup(d => d.Users).Returns(MockDbSetHelper.CreateMockDbSet(users).Object);
    }

    private void SetupJwt(User user)
    {
        _jwt.Setup(j => j.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>()))
            .Returns("test-access-token");
        _jwt.Setup(j => j.GenerateRefreshToken())
            .Returns("test-refresh-token");
    }
}
