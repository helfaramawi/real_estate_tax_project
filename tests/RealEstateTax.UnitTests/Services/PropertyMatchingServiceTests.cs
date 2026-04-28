using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Services;
using RealEstateTax.UnitTests.Helpers;

namespace RealEstateTax.UnitTests.Services;

public class PropertyMatchingServiceTests
{
    private readonly Mock<IApplicationDbContext> _db = new();
    private readonly PropertyMatchingService _sut;

    private static readonly Guid PropertyId1 = Guid.NewGuid();
    private static readonly Guid PropertyId2 = Guid.NewGuid();

    public PropertyMatchingServiceTests()
    {
        // Default: empty sets
        SetupEmptyDb();
        _sut = new PropertyMatchingService(_db.Object, NullLogger<PropertyMatchingService>.Instance);
    }

    // ── FindMatchAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FindMatch_NoMatchingData_ReturnsNoMatch()
    {
        var record = new PropertySourceRecord { Id = Guid.NewGuid() };

        var result = await _sut.FindMatchAsync(record);

        result.IsMatch.Should().BeFalse();
        result.MatchedPropertyId.Should().BeNull();
        result.ConfidenceScore.Should().Be(0.0);
    }

    [Fact]
    public async Task FindMatch_NationalIdMatch_AddsNationalIdFactor()
    {
        var nid = "29901011234567";
        SetupOwnershipByNationalId(nid, PropertyId1);

        var record = new PropertySourceRecord { Id = Guid.NewGuid(), OwnerNationalId = nid };
        var result = await _sut.FindMatchAsync(record);

        result.ContributingFactors.Should().Contain("NationalId");
        result.ConfidenceScore.Should().BeApproximately(0.25, 0.01);
        result.MatchedPropertyId.Should().Be(PropertyId1);
    }

    [Fact]
    public async Task FindMatch_GisMatch_AddsGisProximityFactor()
    {
        // Property at exactly 30.044, 31.235 — source record at same coords
        SetupLocationAt(PropertyId1, 30.044, 31.235);

        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            Latitude = 30.044,
            Longitude = 31.235
        };

        var result = await _sut.FindMatchAsync(record);

        result.ContributingFactors.Should().Contain("GisProximity");
        result.ConfidenceScore.Should().BeApproximately(0.35, 0.01);
    }

    [Fact]
    public async Task FindMatch_GisFarAway_DoesNotMatch()
    {
        // Property at 30.044, 31.235 — source record 10km away
        SetupLocationAt(PropertyId1, 30.044, 31.235);

        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            Latitude = 30.144,  // ~11km away
            Longitude = 31.235
        };

        var result = await _sut.FindMatchAsync(record);

        result.ContributingFactors.Should().NotContain("GisProximity");
    }

    [Fact]
    public async Task FindMatch_MeterNumberMatch_AddsMeterFactor()
    {
        SetupMatchedSourceRecordByMeter("MTR-001", PropertyId1);

        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            MeterNumber = "MTR-001"
        };

        var result = await _sut.FindMatchAsync(record);

        result.ContributingFactors.Should().Contain("MeterNumber");
        result.ConfidenceScore.Should().BeApproximately(0.15, 0.01);
    }

    [Fact]
    public async Task FindMatch_AccountNumberMatch_AddsMeterFactor()
    {
        SetupMatchedSourceRecordByAccount("ACC-999", PropertyId1);

        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            AccountNumber = "ACC-999"
        };

        var result = await _sut.FindMatchAsync(record);

        result.ContributingFactors.Should().Contain("MeterNumber");
    }

    [Fact]
    public async Task FindMatch_AllFourFactors_HighConfidence()
    {
        var nid = "29901011234567";
        SetupOwnershipByNationalId(nid, PropertyId1);
        SetupLocationAt(PropertyId1, 30.044, 31.235);
        SetupPropertyWithAddress(PropertyId1, "12 TAHRIR STREET CAIRO EGYPT");
        SetupMatchedSourceRecordByMeter("MTR-001", PropertyId1);

        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            OwnerNationalId = nid,
            Latitude = 30.044,
            Longitude = 31.235,
            Address = "12 Tahrir Street Cairo Egypt",
            MeterNumber = "MTR-001"
        };

        var result = await _sut.FindMatchAsync(record);

        result.IsMatch.Should().BeTrue();
        result.ConfidenceScore.Should().BeGreaterThan(0.60);
        result.ContributingFactors.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task FindMatch_OnlyOneWeakFactor_BelowThreshold()
    {
        // Only meter match (0.15) — below 0.60 threshold
        SetupMatchedSourceRecordByMeter("MTR-001", PropertyId1);

        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            MeterNumber = "MTR-001"
        };

        var result = await _sut.FindMatchAsync(record);

        result.IsMatch.Should().BeFalse();
        result.ConfidenceScore.Should().BeApproximately(0.15, 0.01);
    }

    // ── CalculateConfidenceScoreAsync ──────────────────────────────────────────

    [Fact]
    public async Task CalculateConfidence_FullyCompleteProperty_ReturnsHighScore()
    {
        var property = new Property
        {
            Id = PropertyId1,
            StreetAddress = "12 Tahrir St",
            City = "Cairo",
            District = "Downtown",
            Governorate = "Cairo",
            Location = new PropertyLocation { Latitude = 30.044, Longitude = 31.235, IsVerified = true },
            Ownerships = [new PropertyOwnership { IsCurrent = true, IsVerified = true }],
            SourceRecords = [new PropertySourceRecord(), new PropertySourceRecord(), new PropertySourceRecord()],
            FieldSurveys = [new FieldSurvey { Status = SurveyStatus.Approved }]
        };

        var score = await _sut.CalculateConfidenceScoreAsync(property);

        score.Should().BeGreaterThan(0.80);
    }

    [Fact]
    public async Task CalculateConfidence_EmptyProperty_ReturnsLowScore()
    {
        var property = new Property
        {
            Id = PropertyId2,
            BuiltUpArea = 100m,
            Ownerships = [],
            SourceRecords = [],
            FieldSurveys = []
        };

        var score = await _sut.CalculateConfidenceScoreAsync(property);

        score.Should().BeLessThan(0.20);
    }

    // ── DetectDuplicatesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DetectDuplicates_NoCandidates_ReturnsEmpty()
    {
        _db.Setup(d => d.Properties).Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<Property>()).Object);

        var property = new Property { Id = PropertyId1, Governorate = "Cairo", City = "Cairo" };
        var duplicates = await _sut.DetectDuplicatesAsync(property);

        duplicates.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectDuplicates_HighSimilarityCandidate_Detected()
    {
        var existing = new Property
        {
            Id = PropertyId2,
            PropertyCode = "P002",
            Governorate = "Cairo",
            City = "Cairo",
            District = "Downtown",
            StreetAddress = "12 Tahrir Street",
            BuiltUpArea = 120m,
            YearBuilt = 2005
        };
        _db.Setup(d => d.Properties).Returns(MockDbSetHelper.CreateMockDbSet(new[] { existing }).Object);

        var target = new Property
        {
            Id = PropertyId1,
            Governorate = "Cairo",
            City = "Cairo",
            District = "Downtown",
            StreetAddress = "12 Tahrir Street",   // identical address
            BuiltUpArea = 120m,
            YearBuilt = 2005
        };

        var duplicates = await _sut.DetectDuplicatesAsync(target);

        duplicates.Should().ContainSingle(d => d.CandidatePropertyId == PropertyId2);
        duplicates.First().SimilarityScore.Should().BeGreaterThanOrEqualTo(0.70);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetupEmptyDb()
    {
        _db.Setup(d => d.PropertyOwnerships)
           .Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<PropertyOwnership>()).Object);
        _db.Setup(d => d.PropertyLocations)
           .Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<PropertyLocation>()).Object);
        _db.Setup(d => d.Properties)
           .Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<Property>()).Object);
        _db.Setup(d => d.PropertySourceRecords)
           .Returns(MockDbSetHelper.CreateMockDbSet(Array.Empty<PropertySourceRecord>()).Object);
    }

    private void SetupOwnershipByNationalId(string nid, Guid propertyId)
    {
        var ownership = new PropertyOwnership
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            IsCurrent = true,
            Taxpayer = new Taxpayer { NationalId = nid }
        };
        _db.Setup(d => d.PropertyOwnerships)
           .Returns(MockDbSetHelper.CreateMockDbSet(new[] { ownership }).Object);
    }

    private void SetupLocationAt(Guid propertyId, double lat, double lon)
    {
        var location = new PropertyLocation
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            Latitude = lat,
            Longitude = lon,
            IsVerified = true
        };
        _db.Setup(d => d.PropertyLocations)
           .Returns(MockDbSetHelper.CreateMockDbSet(new[] { location }).Object);
    }

    private void SetupPropertyWithAddress(Guid propertyId, string fullAddress)
    {
        var property = new Property
        {
            Id = propertyId,
            PropertyCode = "P001",
            FullAddress = fullAddress
        };
        _db.Setup(d => d.Properties)
           .Returns(MockDbSetHelper.CreateMockDbSet(new[] { property }).Object);
    }

    private void SetupMatchedSourceRecordByMeter(string meterNumber, Guid propertyId)
    {
        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            MasterPropertyId = propertyId,
            MeterNumber = meterNumber,
            IsMatched = true
        };
        _db.Setup(d => d.PropertySourceRecords)
           .Returns(MockDbSetHelper.CreateMockDbSet(new[] { record }).Object);
    }

    private void SetupMatchedSourceRecordByAccount(string accountNumber, Guid propertyId)
    {
        var record = new PropertySourceRecord
        {
            Id = Guid.NewGuid(),
            MasterPropertyId = propertyId,
            AccountNumber = accountNumber,
            IsMatched = true
        };
        _db.Setup(d => d.PropertySourceRecords)
           .Returns(MockDbSetHelper.CreateMockDbSet(new[] { record }).Object);
    }
}
