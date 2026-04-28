using FluentAssertions;
using RealEstateTax.Application.DTOs.Auth;
using RealEstateTax.Application.Validators;

namespace RealEstateTax.UnitTests.Validators;

public class AuthValidatorTests
{
    private readonly LoginRequestValidator _loginValidator = new();
    private readonly ChangePasswordRequestValidator _changePasswordValidator = new();

    // ── LoginRequest ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Passes()
    {
        var result = await _loginValidator.ValidateAsync(new LoginRequest("superadmin", "Admin@12345"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password")]      // empty username
    [InlineData("user", "")]          // empty password
    [InlineData("user", "abc")]       // password too short (< 6)
    public async Task Login_InvalidInput_Fails(string username, string password)
    {
        var result = await _loginValidator.ValidateAsync(new LoginRequest(username, password));
        result.IsValid.Should().BeFalse();
    }

    // ── ChangePasswordRequest ──────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_Valid_Passes()
    {
        var result = await _changePasswordValidator.ValidateAsync(
            new ChangePasswordRequest("OldPass@1", "NewPass@2!", "NewPass@2!"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_SameAsOldPassword_Fails()
    {
        var result = await _changePasswordValidator.ValidateAsync(
            new ChangePasswordRequest("Pass@12345", "Pass@12345", "Pass@12345"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public async Task ChangePassword_ConfirmMismatch_Fails()
    {
        var result = await _changePasswordValidator.ValidateAsync(
            new ChangePasswordRequest("Old@12345", "New@12345!", "Different@1"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.ConfirmNewPassword));
    }

    [Theory]
    [InlineData("nouppercase1!")]   // no uppercase
    [InlineData("NOLOWERCASE1!")]   // no lowercase
    [InlineData("NoDigitHere!!")]   // no digit
    [InlineData("NoSpecial123")]    // no special char
    [InlineData("Short1!")]         // too short (< 8 chars)
    public async Task ChangePassword_WeakNewPassword_Fails(string newPassword)
    {
        var result = await _changePasswordValidator.ValidateAsync(
            new ChangePasswordRequest("OldPass@1", newPassword, newPassword));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }
}
