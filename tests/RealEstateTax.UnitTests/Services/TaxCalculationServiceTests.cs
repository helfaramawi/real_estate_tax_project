using FluentAssertions;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;
using RealEstateTax.Infrastructure.Services;

namespace RealEstateTax.UnitTests.Services;

public class TaxCalculationServiceTests
{
    private readonly TaxCalculationService _sut = new();

    private static Property AnyProperty() => new() { Id = Guid.NewGuid(), PropertyCode = "P001", BuiltUpArea = 100 };

    private static Valuation ValuationWith(decimal netValue) => new()
    {
        Id = Guid.NewGuid(),
        NetValue = netValue,
        TaxYear = 2024,
        Status = ValuationStatus.Approved
    };

    private static TaxRule RuleWith(decimal rate, decimal? minThreshold = null) => new()
    {
        Code = "STD",
        TaxRate = rate,
        MinTaxableValue = minThreshold,
        IsActive = true
    };

    // ── CalculateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateAsync_NoExemption_ReturnsCorrectGrossAndNetTax()
    {
        var result = await _sut.CalculateAsync(AnyProperty(), ValuationWith(100_000m), RuleWith(10m), null);

        result.TaxableValue.Should().Be(100_000m);
        result.TaxRate.Should().Be(10m);
        result.GrossTaxAmount.Should().Be(10_000m);
        result.ExemptionAmount.Should().Be(0m);
        result.NetTaxAmount.Should().Be(10_000m);
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateAsync_BelowMinThreshold_ReturnsZeroTax()
    {
        var result = await _sut.CalculateAsync(AnyProperty(), ValuationWith(20_000m), RuleWith(10m, minThreshold: 24_000m), null);

        result.GrossTaxAmount.Should().Be(0m);
        result.NetTaxAmount.Should().Be(0m);
        result.Warnings.Should().ContainSingle(w => w.Contains("below minimum threshold"));
    }

    [Fact]
    public async Task CalculateAsync_WithPercentageExemption_DeductsCorrectly()
    {
        var exemption = new Exemption
        {
            Id = Guid.NewGuid(),
            Status = ExemptionStatus.Approved,
            ExemptionPercentage = 50m,
            Type = ExemptionType.Disabled
        };

        var result = await _sut.CalculateAsync(AnyProperty(), ValuationWith(100_000m), RuleWith(10m), exemption);

        result.GrossTaxAmount.Should().Be(10_000m);
        result.ExemptionAmount.Should().Be(5_000m);
        result.NetTaxAmount.Should().Be(5_000m);
    }

    [Fact]
    public async Task CalculateAsync_WithFixedExemption_CappedAtGrossTax()
    {
        var exemption = new Exemption
        {
            Id = Guid.NewGuid(),
            Status = ExemptionStatus.Approved,
            ExemptAmount = 999_999m,   // larger than gross tax
            Type = ExemptionType.LowIncome
        };

        var result = await _sut.CalculateAsync(AnyProperty(), ValuationWith(100_000m), RuleWith(10m), exemption);

        result.GrossTaxAmount.Should().Be(10_000m);
        result.ExemptionAmount.Should().Be(10_000m);  // capped at gross
        result.NetTaxAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CalculateAsync_PendingExemption_NotApplied()
    {
        var exemption = new Exemption
        {
            Id = Guid.NewGuid(),
            Status = ExemptionStatus.Submitted,
            ExemptionPercentage = 100m,
            Type = ExemptionType.Disabled
        };

        var result = await _sut.CalculateAsync(AnyProperty(), ValuationWith(100_000m), RuleWith(10m), exemption);

        result.ExemptionAmount.Should().Be(0m);
        result.NetTaxAmount.Should().Be(10_000m);
    }

    [Fact]
    public async Task CalculateAsync_AppliedRules_ContainRuleCodeAndRate()
    {
        var result = await _sut.CalculateAsync(AnyProperty(), ValuationWith(50_000m), RuleWith(10m), null);

        result.AppliedRules.Should().Contain(r => r.Contains("STD"));
        result.AppliedRules.Should().Contain(r => r.Contains("10%"));
    }

    // ── CalculatePenaltyAsync ──────────────────────────────────────────────────

    private static TaxBill OverdueBillWith(decimal total, decimal paid, DateTime dueDate) => new()
    {
        Id = Guid.NewGuid(),
        BillNumber = "BILL-001",
        Status = BillStatus.Overdue,
        TotalAmount = total,
        PaidAmount = paid,
        DueDate = dueDate,
        IssueDate = dueDate.AddMonths(-3)
    };

    [Fact]
    public async Task CalculatePenaltyAsync_BillNotOverdue_ReturnsZero()
    {
        var bill = new TaxBill
        {
            Status = BillStatus.Issued,
            TotalAmount = 10_000m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        var penalty = await _sut.CalculatePenaltyAsync(bill, DateTime.UtcNow);

        penalty.Should().Be(0m);
    }

    [Fact]
    public async Task CalculatePenaltyAsync_AsOfDateBeforeDueDate_ReturnsZero()
    {
        var bill = OverdueBillWith(10_000m, 0m, DateTime.UtcNow.AddDays(10));
        var penalty = await _sut.CalculatePenaltyAsync(bill, DateTime.UtcNow);

        penalty.Should().Be(0m);
    }

    [Fact]
    public async Task CalculatePenaltyAsync_OneMonthOverdue_Returns1PercentOfRemaining()
    {
        var dueDate = DateTime.UtcNow.AddDays(-31);
        var bill = OverdueBillWith(10_000m, 0m, dueDate);

        var penalty = await _sut.CalculatePenaltyAsync(bill, DateTime.UtcNow);

        // 1 month × 1% = 1% of 10,000 = 100
        penalty.Should().Be(100m);
    }

    [Fact]
    public async Task CalculatePenaltyAsync_PartiallyPaid_PenaltyOnRemainingOnly()
    {
        var dueDate = DateTime.UtcNow.AddDays(-31);
        var bill = OverdueBillWith(10_000m, 6_000m, dueDate);  // 4,000 remaining

        var penalty = await _sut.CalculatePenaltyAsync(bill, DateTime.UtcNow);

        // 1% of 4,000 remaining = 40
        penalty.Should().Be(40m);
    }

    [Fact]
    public async Task CalculatePenaltyAsync_VeryLongOverdue_CappedAt50Percent()
    {
        var dueDate = DateTime.UtcNow.AddYears(-10);  // 120+ months
        var bill = OverdueBillWith(10_000m, 0m, dueDate);

        var penalty = await _sut.CalculatePenaltyAsync(bill, DateTime.UtcNow);

        // Cap is 50% of 10,000 = 5,000
        penalty.Should().Be(5_000m);
    }
}
