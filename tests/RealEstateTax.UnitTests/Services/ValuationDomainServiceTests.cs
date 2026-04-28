using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Services;

namespace RealEstateTax.UnitTests.Services;

public class ValuationDomainServiceTests
{
    private readonly ValuationDomainService _sut = new(NullLogger<ValuationDomainService>.Instance);

    private static Property PropertyWith(decimal builtUpArea) => new()
    {
        Id = Guid.NewGuid(),
        PropertyCode = "P001",
        BuiltUpArea = builtUpArea,
        Type = PropertyType.Residential
    };

    private static ValuationRule RuleWith(decimal? rentPerSqM, decimal deductionPct, decimal? minNet = null, decimal? maxNet = null) => new()
    {
        Code = "VR01",
        StandardRentPerSqM = rentPerSqM,
        DeductionPercentage = deductionPct,
        MinNetValue = minNet,
        MaxNetValue = maxNet
    };

    // ── Rental Value method ────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeValue_RentalValue_CorrectGrossAndNet()
    {
        // 100 m² × 50 EGP/m²/month × 12 months = 60,000 ARV
        // Deduction 30% → Net = 42,000
        var result = await _sut.ComputeValueAsync(PropertyWith(100m), ValuationMethod.RentalValue, RuleWith(50m, 30m));

        result.GrossValue.Should().Be(60_000m);
        result.DeductionPercentage.Should().Be(30m);
        result.NetValue.Should().Be(42_000m);
        result.MethodUsed.Should().Be("RentalValue");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ComputeValue_RentalValue_NoStandardRent_ReturnsZeroWithWarning()
    {
        var result = await _sut.ComputeValueAsync(PropertyWith(100m), ValuationMethod.RentalValue, RuleWith(null, 30m));

        result.GrossValue.Should().Be(0m);
        result.NetValue.Should().Be(0m);
        result.Warnings.Should().ContainSingle(w => w.Contains("StandardRentPerSqM not configured"));
    }

    [Fact]
    public async Task ComputeValue_RentalValue_NetBelowMin_FloorsToMin()
    {
        // Net would be 42,000 but min is 50,000
        var result = await _sut.ComputeValueAsync(PropertyWith(100m), ValuationMethod.RentalValue, RuleWith(50m, 30m, minNet: 50_000m));

        result.NetValue.Should().Be(50_000m);
        result.Warnings.Should().ContainSingle(w => w.Contains("floored to minimum"));
    }

    [Fact]
    public async Task ComputeValue_RentalValue_NetAboveMax_CapsToMax()
    {
        // Net would be 42,000 but max is 30,000
        var result = await _sut.ComputeValueAsync(PropertyWith(100m), ValuationMethod.RentalValue, RuleWith(50m, 30m, maxNet: 30_000m));

        result.NetValue.Should().Be(30_000m);
        result.Warnings.Should().ContainSingle(w => w.Contains("capped to maximum"));
    }

    [Fact]
    public async Task ComputeValue_RentalValue_DefaultDeductionIs30Percent_WhenRuleDeductionIsZero()
    {
        // DeductionPercentage = 0 → fallback to 30%
        var result = await _sut.ComputeValueAsync(PropertyWith(100m), ValuationMethod.RentalValue, RuleWith(50m, 0m));

        result.DeductionPercentage.Should().Be(30m);
        result.NetValue.Should().Be(42_000m);
    }

    // ── Unimplemented methods return warnings ──────────────────────────────────

    [Theory]
    [InlineData(ValuationMethod.MarketComparison)]
    [InlineData(ValuationMethod.CostApproach)]
    public async Task ComputeValue_UnimplementedMethod_ReturnsZeroWithWarning(ValuationMethod method)
    {
        var result = await _sut.ComputeValueAsync(PropertyWith(100m), method, RuleWith(50m, 30m));

        result.NetValue.Should().Be(0m);
        result.Warnings.Should().NotBeEmpty();
    }
}
