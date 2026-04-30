using FluentAssertions;
using Moq;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Services;
using RealEstateTax.UnitTests.Helpers;

namespace RealEstateTax.UnitTests.Services;

public class RiskScoringServiceTests
{
    private readonly Mock<IApplicationDbContext> _db = new();
    private readonly RiskScoringService _sut;

    public RiskScoringServiceTests()
    {
        // Default: empty valuations and bills
        _db.Setup(d => d.Valuations).Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<Valuation>()).Object);
        _db.Setup(d => d.TaxBills).Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<TaxBill>()).Object);
        _sut = new RiskScoringService(_db.Object);
    }

    // ── ClassifyRiskLevel ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, RiskLevel.Low)]
    [InlineData(24.99, RiskLevel.Low)]
    [InlineData(25.0, RiskLevel.Medium)]
    [InlineData(49.99, RiskLevel.Medium)]
    [InlineData(50.0, RiskLevel.High)]
    [InlineData(74.99, RiskLevel.High)]
    [InlineData(75.0, RiskLevel.Critical)]
    [InlineData(100.0, RiskLevel.Critical)]
    public void ClassifyRiskLevel_ReturnsCorrectLevel(double score, RiskLevel expected)
    {
        _sut.ClassifyRiskLevel(score).Should().Be(expected);
    }

    // ── ComputeRiskScoreAsync – data completeness ──────────────────────────────

    [Fact]
    public async Task ComputeRiskScore_FullyCompleteProperty_HasLowRisk()
    {
        var property = BuildCompleteProperty();

        var result = await _sut.ComputeRiskScoreAsync(property);

        result.Level.Should().Be(RiskLevel.Low);
        result.OverallScore.Should().BeLessThan(25.0);
    }

    [Fact]
    public async Task ComputeRiskScore_MissingLocation_IncreasesRisk()
    {
        var withLocation = BuildCompleteProperty();
        var withoutLocation = BuildCompleteProperty();
        withoutLocation.Location = null;

        var scoreWith = (await _sut.ComputeRiskScoreAsync(withLocation)).OverallScore;
        var scoreWithout = (await _sut.ComputeRiskScoreAsync(withoutLocation)).OverallScore;

        scoreWithout.Should().BeGreaterThan(scoreWith);
    }

    [Fact]
    public async Task ComputeRiskScore_NoCurrentOwner_AddsRiskFactor()
    {
        var property = BuildCompleteProperty();
        foreach (var o in property.Ownerships)
            o.IsCurrent = false;

        var result = await _sut.ComputeRiskScoreAsync(property);

        result.RiskFactors.Should().Contain(f => f.Contains("NoCurrentOwner") || f.Contains("NoCurrentOwnership"));
    }

    [Fact]
    public async Task ComputeRiskScore_WithOverdueBills_PaymentScoreReflectsRisk()
    {
        var property = BuildCompleteProperty();
        var overdueBill = new TaxBill
        {
            Id = Guid.NewGuid(),
            PropertyId = property.Id,
            Status = BillStatus.Overdue,
            TotalAmount = 5000m,
            PaidAmount = 0m,
            IssueDate = DateTime.UtcNow.AddYears(-1),
            DueDate = DateTime.UtcNow.AddMonths(-6)
        };
        _db.Setup(d => d.TaxBills)
           .Returns(MockDbSetHelper.CreateMockDbSet(new[] { overdueBill }).Object);

        var result = await _sut.ComputeRiskScoreAsync(property);

        result.RiskFactors.Should().Contain(f => f.Contains("OverdueBills"));
    }

    [Fact]
    public async Task ComputeRiskScore_NoValuation_AddsNoValuationFactor()
    {
        var property = BuildCompleteProperty();
        // DB returns empty valuations (default setup)

        var result = await _sut.ComputeRiskScoreAsync(property);

        result.RiskFactors.Should().Contain(f => f.Contains("NoValuationOnRecord"));
    }

    [Fact]
    public async Task ComputeRiskScore_ResultHasAllSixFactorScores()
    {
        var property = BuildCompleteProperty();
        var result = await _sut.ComputeRiskScoreAsync(property);

        result.DataCompletenessScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        result.ValuationConsistencyScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        result.OwnershipChainScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        result.PaymentHistoryScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        result.GeoVerificationScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        result.SourceConsistencyScore.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Property BuildCompleteProperty()
    {
        var id = Guid.NewGuid();
        var taxpayerId = Guid.NewGuid();
        return new Property
        {
            Id = id,
            PropertyCode = "P001",
            BuiltUpArea = 120m,
            YearBuilt = 2005,
            StreetAddress = "12 Tahrir St",
            Governorate = "Cairo",
            Location = new PropertyLocation
            {
                Id = Guid.NewGuid(),
                PropertyId = id,
                Latitude = 30.044,
                Longitude = 31.235,
                IsVerified = true
            },
            Ownerships =
            [
                new PropertyOwnership
                {
                    Id = Guid.NewGuid(),
                    PropertyId = id,
                    TaxpayerId = taxpayerId,
                    IsCurrent = true,
                    IsVerified = true,
                    OwnershipPercentage = 100m,
                    TitleDeedNumber = "DEED-12345"
                }
            ],
            SourceRecords =
            [
                new PropertySourceRecord { Id = Guid.NewGuid(), MasterPropertyId = id, AreaFromSource = 120m },
                new PropertySourceRecord { Id = Guid.NewGuid(), MasterPropertyId = id, AreaFromSource = 121m }
            ]
        };
    }
}
