using FluentAssertions;
using RealEstateTax.Application.DTOs.Properties;
using RealEstateTax.Application.Validators;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.UnitTests.Validators;

public class PropertyValidatorTests
{
    private readonly CreatePropertyRequestValidator _validator = new();

    private static CreatePropertyRequest Cairo() => new()
    {
        Type = PropertyType.Residential,
        BuiltUpArea = 120m,
        Latitude = 30.044,
        Longitude = 31.235
    };

    private static CreatePropertyRequest AtCoords(double lat, double lon) => new()
    {
        Type = PropertyType.Residential,
        BuiltUpArea = 120m,
        Latitude = lat,
        Longitude = lon
    };

    // ── Egypt bounding box ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(22.0, 24.7)]    // SW corner (valid)
    [InlineData(31.9, 37.1)]    // NE corner (valid)
    [InlineData(30.0, 31.0)]    // Cairo (valid)
    [InlineData(25.7, 32.5)]    // Luxor area (valid)
    public async Task Coordinates_WithinEgyptBounds_Pass(double lat, double lon)
    {
        var result = await _validator.ValidateAsync(AtCoords(lat, lon));
        result.Errors.Should().NotContain(e =>
            e.PropertyName == nameof(CreatePropertyRequest.Latitude) ||
            e.PropertyName == nameof(CreatePropertyRequest.Longitude));
    }

    [Theory]
    [InlineData(21.9, 31.0)]    // south of Egypt
    [InlineData(32.0, 31.0)]    // north of Egypt
    [InlineData(30.0, 24.6)]    // west of Egypt
    [InlineData(30.0, 37.2)]    // east of Egypt
    public async Task Coordinates_OutsideEgyptBounds_Fail(double lat, double lon)
    {
        var result = await _validator.ValidateAsync(AtCoords(lat, lon));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Coordinates_Null_AreOptional_Passes()
    {
        var req = new CreatePropertyRequest
        {
            Type = PropertyType.Residential,
            BuiltUpArea = 120m,
            Latitude = null,
            Longitude = null
        };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.Should().BeTrue();
    }

    // ── BuiltUpArea ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BuiltUpArea_Zero_Fails()
    {
        var req = new CreatePropertyRequest { Type = PropertyType.Residential, BuiltUpArea = 0m };
        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyRequest.BuiltUpArea));
    }

    [Fact]
    public async Task BuiltUpArea_Negative_Fails()
    {
        var req = new CreatePropertyRequest { Type = PropertyType.Residential, BuiltUpArea = -1m };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task BuiltUpArea_ExceedsMaximum_Fails()
    {
        var req = new CreatePropertyRequest { Type = PropertyType.Residential, BuiltUpArea = 1_000_001m };
        var result = await _validator.ValidateAsync(req);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    [InlineData(1_000_000)]
    public async Task BuiltUpArea_ValidRange_Passes(decimal area)
    {
        var req = new CreatePropertyRequest { Type = PropertyType.Residential, BuiltUpArea = area };
        var result = await _validator.ValidateAsync(req);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreatePropertyRequest.BuiltUpArea));
    }

    // ── YearBuilt ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task YearBuilt_FutureYear_Fails()
    {
        var req = Cairo();
        req.YearBuilt = DateTime.UtcNow.Year + 1;
        var result = await _validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePropertyRequest.YearBuilt));
    }

    [Fact]
    public async Task YearBuilt_Before1800_Fails()
    {
        var req = Cairo();
        req.YearBuilt = 1799;
        var result = await _validator.ValidateAsync(req);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task YearBuilt_Null_IsOptional_Passes()
    {
        var req = Cairo();
        req.YearBuilt = null;
        var result = await _validator.ValidateAsync(req);
        result.IsValid.Should().BeTrue();
    }
}
