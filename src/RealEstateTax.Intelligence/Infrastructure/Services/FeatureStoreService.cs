using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Intelligence.Application.Interfaces;
using RealEstateTax.Intelligence.Domain.Entities;
using RealEstateTax.Intelligence.Infrastructure.Persistence;
using RealEstateTax.Infrastructure.Persistence;

namespace RealEstateTax.Intelligence.Infrastructure.Services;

public class FeatureStoreService(
    ApplicationDbContext appDb,
    IntelligenceDbContext intelDb,
    ILogger<FeatureStoreService> logger) : IFeatureStoreService
{
    public async Task<List<FeatureVector>> ComputeFeaturesAsync(
        IReadOnlyList<Guid> propertyIds, string featureVersion, CancellationToken ct = default)
    {
        logger.LogDebug("Computing features for {Count} properties at version {Version}",
            propertyIds.Count, featureVersion);

        var raw = await appDb.Properties
            .Where(p => propertyIds.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                BuiltUpArea = (double?)p.BuiltUpArea,
                LandArea = (double?)p.LandArea,
                PropertyTypeCode = (int)p.Type,
                p.YearBuilt,
                FloorsCount = p.NumberOfFloors,
                UnitsCount = p.NumberOfUnits,

                Lat = (double?)p.Location!.Latitude,
                Lon = (double?)p.Location!.Longitude,
                HasBoundary = p.Location != null && p.Location.Boundary != null,

                DeclaredAnnualValue = p.Valuations
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(v => (double?)v.AnnualRentalValue)
                    .FirstOrDefault(),
                MarketValuePerSqm = p.Valuations
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(v => (double?)v.MarketValuePerSqM)
                    .FirstOrDefault(),
                CapRate = p.Valuations
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(v => (double?)v.CapitalizationRate)
                    .FirstOrDefault(),

                BillsCount = p.TaxBills.Count(),
                PaidCount = p.TaxBills.Count(b => b.Status == RealEstateTax.Domain.Enums.BillStatus.Paid),
                OverdueCount = p.TaxBills.Count(b => b.Status == RealEstateTax.Domain.Enums.BillStatus.Overdue),
                TotalPaid = (double?)p.TaxBills.SelectMany(b => b.Payments).Sum(pay => (decimal)pay.Amount),
                TotalOutstanding = (double?)p.TaxBills
                    .Where(b => b.Status == RealEstateTax.Domain.Enums.BillStatus.Overdue)
                    .Sum(b => (decimal)b.TotalAmount),

                SurveysCount = p.FieldSurveys.Count(),
                DaysSinceLastSurvey = p.FieldSurveys.Any()
                    ? (int?)(int)(DateTime.UtcNow - p.FieldSurveys.Max(s => s.CreatedAt)).TotalDays
                    : null,
                GpsAccuracyAvg = p.FieldSurveys.Average(s => (double?)s.GpsAccuracy),

                RiskScore = p.RiskScores.OrderByDescending(r => r.CreatedAt).Select(r => (int?)r.Score).FirstOrDefault(),
                GeoVerScore = p.RiskScores.OrderByDescending(r => r.CreatedAt).Select(r => (int?)r.GeoVerificationScore).FirstOrDefault(),
                FraudCount = p.FraudFlags.Count(f => f.Status != RealEstateTax.Domain.Enums.FraudFlagStatus.Dismissed),
                AppealsCount = p.Appeals.Count(),
                ExemptionsCount = p.Exemptions.Count(),

                SourceCount = p.SourceRecords.Count(),
                MatchedCount = p.SourceRecords.Count(sr => sr.IsMatched),
                MaxConfidence = p.SourceRecords.Max(sr => (double?)sr.MatchConfidenceScore),

                OwnershipCount = p.Ownerships.Count(o => o.IsCurrent),
                DaysSinceTransfer = p.Ownerships.Any()
                    ? (int?)(int)(DateTime.UtcNow - p.Ownerships.Max(o => o.CreatedAt)).TotalDays
                    : null,
                HasCorporate = p.Ownerships.Any(o =>
                    o.OwnershipType == RealEstateTax.Domain.Enums.OwnershipType.Corporate && o.IsCurrent),
            })
            .AsNoTracking()
            .ToListAsync(ct);

        return raw.Select(p => new FeatureVector
        {
            PropertyId = p.Id,
            FeatureVersion = featureVersion,
            ComputedAt = DateTime.UtcNow,
            Lat = p.Lat,
            Lon = p.Lon,
            HasBoundaryPolygon = p.HasBoundary,
            BuiltUpArea = p.BuiltUpArea,
            LandArea = p.LandArea,
            PropertyTypeCode = p.PropertyTypeCode,
            YearBuilt = p.YearBuilt,
            FloorsCount = p.FloorsCount,
            UnitsCount = p.UnitsCount,
            DeclaredAnnualValue = p.DeclaredAnnualValue,
            MarketValuePerSqm = p.MarketValuePerSqm,
            CapitalizationRate = p.CapRate,
            BillsCount = p.BillsCount,
            PaidOnTimeRate = p.BillsCount > 0 ? (double)p.PaidCount / p.BillsCount : null,
            OverdueCount = p.OverdueCount,
            TotalPaidEgp = p.TotalPaid,
            TotalOutstandingEgp = p.TotalOutstanding,
            SurveysCount = p.SurveysCount,
            DaysSinceLastSurvey = p.DaysSinceLastSurvey,
            GpsAccuracyAvg = p.GpsAccuracyAvg,
            ExistingRiskScore = p.RiskScore,
            GeoVerificationScore = p.GeoVerScore,
            FraudFlagsCount = p.FraudCount,
            AppealsCount = p.AppealsCount,
            ExemptionsCount = p.ExemptionsCount,
            SourceRecordsCount = p.SourceCount,
            MatchedRecordsCount = p.MatchedCount,
            MaxMatchConfidence = p.MaxConfidence,
            OwnershipChainLength = p.OwnershipCount,
            DaysSinceLastTransfer = p.DaysSinceTransfer,
            CorporateOwnerFlag = p.HasCorporate,
        }).ToList();
    }

    public async Task BulkUpsertAsync(List<FeatureVector> vectors, CancellationToken ct = default)
    {
        foreach (var v in vectors)
        {
            var existing = await intelDb.FeatureVectors
                .FirstOrDefaultAsync(x => x.PropertyId == v.PropertyId && x.FeatureVersion == v.FeatureVersion, ct);

            if (existing is null)
                intelDb.FeatureVectors.Add(v);
            else
            {
                intelDb.Entry(existing).CurrentValues.SetValues(v);
                existing.ComputedAt = DateTime.UtcNow;
            }
        }
        await intelDb.SaveChangesAsync(ct);
    }

    public Task<List<FeatureVector>> GetByVersionAsync(string featureVersion, int skip, int take, CancellationToken ct = default)
        => intelDb.FeatureVectors
            .Where(f => f.FeatureVersion == featureVersion)
            .OrderBy(f => f.PropertyId)
            .Skip(skip).Take(take)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<int> CountByVersionAsync(string featureVersion, CancellationToken ct = default)
        => intelDb.FeatureVectors.CountAsync(f => f.FeatureVersion == featureVersion, ct);
}
