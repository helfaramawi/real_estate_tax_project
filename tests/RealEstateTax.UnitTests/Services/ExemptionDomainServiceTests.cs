using FluentAssertions;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Services;

namespace RealEstateTax.UnitTests.Services;

public class ExemptionDomainServiceTests
{
    private readonly ExemptionDomainService _sut = new();

    private static Property ResidentialProperty(decimal area = 80m) => new()
    {
        Id = Guid.NewGuid(),
        PropertyCode = "P001",
        Type = PropertyType.Residential,
        BuiltUpArea = area,
        YearBuilt = 2020
    };

    private static Taxpayer Individual() => new()
    {
        Id = Guid.NewGuid(),
        NationalId = "29901011234567",
        IsCorporate = false,
        FirstName = "Ahmed",
        LastName = "Hassan"
    };

    private static ExemptionRule ActiveRule(string type, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        Code = "RULE01",
        ExemptionType = type,
        IsActive = isActive
    };

    // ── Eligibility checks ─────────────────────────────────────────────────────

    [Fact]
    public async Task CheckEligibility_InactiveRule_ReturnsFalse()
    {
        var result = await _sut.CheckEligibilityAsync(
            ResidentialProperty(), Individual(),
            ActiveRule(nameof(ExemptionType.SocialHousing), isActive: false));

        result.IsEligible.Should().BeFalse();
        result.FailedCriteria.Should().Contain(c => c.Contains("inactive"));
    }

    [Fact]
    public async Task CheckEligibility_SocialHousing_SmallProperty_IsEligible()
    {
        var result = await _sut.CheckEligibilityAsync(
            ResidentialProperty(80m), Individual(),
            ActiveRule(nameof(ExemptionType.SocialHousing)));

        result.IsEligible.Should().BeTrue();
        result.SatisfiedCriteria.Should().Contain(c => c.Contains("≤100m²"));
    }

    [Fact]
    public async Task CheckEligibility_SocialHousing_LargeProperty_IsNotEligible()
    {
        var result = await _sut.CheckEligibilityAsync(
            ResidentialProperty(150m), Individual(),
            ActiveRule(nameof(ExemptionType.SocialHousing)));

        result.IsEligible.Should().BeFalse();
        result.FailedCriteria.Should().Contain(c => c.Contains("ExceedsSocialHousingLimit"));
    }

    [Fact]
    public async Task CheckEligibility_SocialHousing_NonResidential_IsNotEligible()
    {
        var commercial = ResidentialProperty(80m);
        commercial.Type = PropertyType.Commercial;

        var result = await _sut.CheckEligibilityAsync(
            commercial, Individual(),
            ActiveRule(nameof(ExemptionType.SocialHousing)));

        result.IsEligible.Should().BeFalse();
        result.FailedCriteria.Should().Contain(c => c.Contains("NotResidential"));
    }

    [Fact]
    public async Task CheckEligibility_Agricultural_AgriculturalProperty_IsEligible()
    {
        var farm = ResidentialProperty(5000m);
        farm.Type = PropertyType.Agricultural;

        var result = await _sut.CheckEligibilityAsync(
            farm, Individual(),
            ActiveRule(nameof(ExemptionType.Agricultural)));

        result.IsEligible.Should().BeTrue();
    }

    [Fact]
    public async Task CheckEligibility_DisabledOwner_Corporate_IsNotEligible()
    {
        var corporate = Individual();
        corporate.IsCorporate = true;

        var result = await _sut.CheckEligibilityAsync(
            ResidentialProperty(), corporate,
            ActiveRule(nameof(ExemptionType.DisabledOwner)));

        result.IsEligible.Should().BeFalse();
        result.FailedCriteria.Should().Contain(c => c.Contains("Corporate"));
    }

    [Fact]
    public async Task CheckEligibility_NewConstruction_RecentBuild_IsEligible()
    {
        var property = ResidentialProperty();
        property.YearBuilt = DateTime.UtcNow.Year - 1;

        var result = await _sut.CheckEligibilityAsync(
            property, Individual(),
            ActiveRule(nameof(ExemptionType.NewConstruction)));

        result.IsEligible.Should().BeTrue();
        result.SatisfiedCriteria.Should().Contain(c => c.Contains("ConstructedWithin2Years"));
    }

    [Fact]
    public async Task CheckEligibility_NewConstruction_OldBuild_IsNotEligible()
    {
        var property = ResidentialProperty();
        property.YearBuilt = 2010;

        var result = await _sut.CheckEligibilityAsync(
            property, Individual(),
            ActiveRule(nameof(ExemptionType.NewConstruction)));

        result.IsEligible.Should().BeFalse();
    }

    // ── CalculateExemptionAmount ───────────────────────────────────────────────

    [Fact]
    public async Task CalculateExemptionAmount_PercentageBased_CorrectAmount()
    {
        var exemption = new Exemption
        {
            Id = Guid.NewGuid(),
            Status = ExemptionStatus.Approved,
            ExemptionPercentage = 50m
        };

        var amount = await _sut.CalculateExemptionAmountAsync(10_000m, exemption);

        amount.Should().Be(5_000m);
    }

    [Fact]
    public async Task CalculateExemptionAmount_FixedAmount_CappedAtGross()
    {
        var exemption = new Exemption
        {
            Id = Guid.NewGuid(),
            Status = ExemptionStatus.Approved,
            ExemptAmount = 999_999m
        };

        var amount = await _sut.CalculateExemptionAmountAsync(10_000m, exemption);

        amount.Should().Be(10_000m);  // capped at gross tax
    }

    [Fact]
    public async Task CalculateExemptionAmount_NotApproved_ReturnsZero()
    {
        var exemption = new Exemption
        {
            Id = Guid.NewGuid(),
            Status = ExemptionStatus.Submitted,
            ExemptionPercentage = 100m
        };

        var amount = await _sut.CalculateExemptionAmountAsync(10_000m, exemption);

        amount.Should().Be(0m);
    }
}
