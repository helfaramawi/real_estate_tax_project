using FluentAssertions;
using RealEstateTax.Application.Validators;

namespace RealEstateTax.UnitTests.Validators;

public class NidValidatorTests
{
    // ── Structural validity ────────────────────────────────────────────────────

    [Theory]
    [InlineData("29901011234567")]  // 1999-01-01, gov 12
    [InlineData("30001011234567")]  // 2000-01-01, gov 12
    [InlineData("29912311234567")]  // 1999-12-31, gov 12
    [InlineData("27507151001234")]  // 1975-07-15, gov 10
    public void IsStructurallyValid_ValidNid_ReturnsTrue(string nid)
    {
        EgyptianNidValidator.IsStructurallyValid(nid).Should().BeTrue();
    }

    [Theory]
    [InlineData("19901011234567")]  // century digit = 1 (invalid)
    [InlineData("49901011234567")]  // century digit = 4 (invalid)
    [InlineData("29913011234567")]  // month = 13 (invalid)
    [InlineData("29900011234567")]  // month = 00 (invalid)
    [InlineData("29901321234567")]  // day = 32 (invalid)
    [InlineData("29901001234567")]  // day = 00 (invalid)
    [InlineData("29902291234567")]  // 1999-02-29 (not a leap year)
    [InlineData("29901014000567")]  // governorate 40 (invalid)
    [InlineData("29901010001234")]  // governorate 00 (invalid)
    [InlineData("2990101123456")]   // 13 digits
    [InlineData("299010112345678")] // 15 digits
    [InlineData("2990101123456A")]  // contains letter
    [InlineData(null)]
    [InlineData("")]
    public void IsStructurallyValid_InvalidNid_ReturnsFalse(string? nid)
    {
        EgyptianNidValidator.IsStructurallyValid(nid).Should().BeFalse();
    }

    [Theory]
    [InlineData("30002291234567")]  // 2000-02-29 (2000 IS a leap year, century digit 3)
    [InlineData("29602291234567")]  // 1996-02-29 (1996 IS a leap year)
    public void IsStructurallyValid_LeapYearDate_ReturnsTrue(string nid)
    {
        EgyptianNidValidator.IsStructurallyValid(nid).Should().BeTrue();
    }

    // ── Governorate boundary tests ─────────────────────────────────────────────

    [Theory]
    [InlineData("29901010112345")]  // gov 01 (Cairo)
    [InlineData("29901013512345")]  // gov 35 (max valid)
    public void IsStructurallyValid_BoundaryGovernorates_ReturnTrue(string nid)
    {
        EgyptianNidValidator.IsStructurallyValid(nid).Should().BeTrue();
    }

    [Theory]
    [InlineData("29901013612345")]  // gov 36 (invalid)
    [InlineData("29901019912345")]  // gov 99 (invalid)
    public void IsStructurallyValid_InvalidGovernorates_ReturnFalse(string nid)
    {
        EgyptianNidValidator.IsStructurallyValid(nid).Should().BeFalse();
    }
}
