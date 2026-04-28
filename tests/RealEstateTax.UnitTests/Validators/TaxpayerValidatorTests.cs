using FluentAssertions;
using RealEstateTax.Application.DTOs.Taxpayers;
using RealEstateTax.Application.Validators;

namespace RealEstateTax.UnitTests.Validators;

public class TaxpayerValidatorTests
{
    private readonly CreateTaxpayerRequestValidator _validator = new();

    private static CreateTaxpayerRequest Individual(string nationalId, string? phone = null, string? email = null) => new()
    {
        NationalId = nationalId,
        IsCorporate = false,
        FirstName = "Ahmed",
        LastName = "Hassan",
        PhoneNumber = phone,
        Email = email
    };

    // ── National ID ────────────────────────────────────────────────────────────

    [Fact]
    public async Task NationalId_Exactly14Digits_Passes()
    {
        var result = await _validator.ValidateAsync(Individual("29901011234567"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123")]          // 13 digits
    [InlineData("123456789012345")]        // 15 digits
    [InlineData("2990101123456A")]         // contains letter
    [InlineData("2990101 123456")]         // contains space
    public async Task NationalId_Invalid_Fails(string nid)
    {
        var result = await _validator.ValidateAsync(Individual(nid));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaxpayerRequest.NationalId));
    }

    // ── Phone number ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("01012345678")]    // Vodafone 010
    [InlineData("01112345678")]    // Orange 011
    [InlineData("01212345678")]    // Etisalat 012
    [InlineData("01512345678")]    // WE 015
    [InlineData("+201012345678")] // international +20
    [InlineData("201012345678")]  // international without +
    public async Task PhoneNumber_ValidEgyptianMobile_Passes(string phone)
    {
        var result = await _validator.ValidateAsync(Individual("29901011234567", phone));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateTaxpayerRequest.PhoneNumber));
    }

    [Theory]
    [InlineData("0201012345678")]   // wrong prefix
    [InlineData("0901012345678")]   // 09x not valid
    [InlineData("1234")]            // too short
    [InlineData("abcdefghijk")]     // not a number
    public async Task PhoneNumber_Invalid_Fails(string phone)
    {
        var result = await _validator.ValidateAsync(Individual("29901011234567", phone));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaxpayerRequest.PhoneNumber));
    }

    [Fact]
    public async Task PhoneNumber_Null_IsOptional_Passes()
    {
        var result = await _validator.ValidateAsync(Individual("29901011234567", null));
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateTaxpayerRequest.PhoneNumber));
    }

    // ── Corporate taxpayer ────────────────────────────────────────────────────

    [Fact]
    public async Task Corporate_WithCompanyName_Passes()
    {
        var request = new CreateTaxpayerRequest
        {
            NationalId = "12345678901234",
            IsCorporate = true,
            CompanyName = "Cairo Properties LLC"
        };

        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Corporate_MissingCompanyName_Fails()
    {
        var request = new CreateTaxpayerRequest
        {
            NationalId = "12345678901234",
            IsCorporate = true,
            CompanyName = ""
        };

        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaxpayerRequest.CompanyName));
    }

    [Fact]
    public async Task Email_Invalid_Fails()
    {
        var result = await _validator.ValidateAsync(Individual("29901011234567", email: "not-an-email"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateTaxpayerRequest.Email));
    }
}
