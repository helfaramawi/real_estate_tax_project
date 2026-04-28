using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Services;

namespace RealEstateTax.Infrastructure.Services;

public class PropertyMatchingService : IPropertyMatchingService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<PropertyMatchingService> _logger;

    // Confidence weight configuration — tune with domain experts
    private const double GisWeight = 0.35;
    private const double AddressWeight = 0.25;
    private const double NationalIdWeight = 0.25;
    private const double MeterWeight = 0.15;

    private const double GisMatchRadiusMeters = 50.0;
    private const double AddressSimilarityThreshold = 0.70;

    public PropertyMatchingService(IApplicationDbContext db, ILogger<PropertyMatchingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MatchResult> FindMatchAsync(PropertySourceRecord sourceRecord, CancellationToken ct = default)
    {
        var factors = new List<string>();
        double totalScore = 0.0;
        Guid? bestMatchId = null;

        // 1. National ID match — strongest signal when available
        if (!string.IsNullOrEmpty(sourceRecord.OwnerNationalId))
        {
            var nationalIdMatch = await FindByNationalIdAsync(sourceRecord.OwnerNationalId, ct);
            if (nationalIdMatch.HasValue)
            {
                totalScore += NationalIdWeight;
                bestMatchId = nationalIdMatch;
                factors.Add("NationalId");
            }
        }

        // 2. GIS proximity match
        if (sourceRecord.Latitude.HasValue && sourceRecord.Longitude.HasValue)
        {
            var gisMatch = await FindByGisAsync(sourceRecord.Latitude.Value, sourceRecord.Longitude.Value, ct);
            if (gisMatch.HasValue)
            {
                totalScore += GisWeight;
                bestMatchId ??= gisMatch;
                factors.Add("GisProximity");
            }
        }

        // 3. Address similarity
        if (!string.IsNullOrEmpty(sourceRecord.Address))
        {
            var addressMatch = await FindByAddressSimilarityAsync(sourceRecord.Address, ct);
            if (addressMatch.HasValue)
            {
                totalScore += AddressWeight;
                bestMatchId ??= addressMatch;
                factors.Add("AddressSimilarity");
            }
        }

        // 4. Meter/account number match
        if (!string.IsNullOrEmpty(sourceRecord.MeterNumber) || !string.IsNullOrEmpty(sourceRecord.AccountNumber))
        {
            var meterMatch = await FindByMeterNumberAsync(sourceRecord.MeterNumber, sourceRecord.AccountNumber, ct);
            if (meterMatch.HasValue)
            {
                totalScore += MeterWeight;
                bestMatchId ??= meterMatch;
                factors.Add("MeterNumber");
            }
        }

        var isMatch = totalScore >= 0.60 && bestMatchId.HasValue;
        var method = factors.Count > 0 ? string.Join("+", factors) : "NoMatch";

        return new MatchResult(isMatch, isMatch ? bestMatchId : null, totalScore, method, [.. factors]);
    }

    public async Task<double> CalculateConfidenceScoreAsync(Property property, CancellationToken ct = default)
    {
        double score = 0.0;
        double maxScore = 0.0;

        // GIS coordinates present and verified
        maxScore += 0.20;
        if (property.Location?.Coordinates != null) score += 0.20;
        else if (property.Location?.Latitude.HasValue == true) score += 0.10;

        // Owner linked and verified
        maxScore += 0.20;
        var currentOwnership = property.Ownerships.FirstOrDefault(o => o.IsCurrent);
        if (currentOwnership != null)
        {
            score += currentOwnership.IsVerified ? 0.20 : 0.10;
        }

        // Multiple source records confirm the property
        maxScore += 0.20;
        var sourceCount = property.SourceRecords.Count;
        score += sourceCount >= 3 ? 0.20 : sourceCount >= 2 ? 0.15 : sourceCount >= 1 ? 0.08 : 0.0;

        // Field survey completed
        maxScore += 0.20;
        var approvedSurvey = property.FieldSurveys.Any(s => s.Status == Domain.Enums.SurveyStatus.Approved);
        if (approvedSurvey) score += 0.20;
        else if (property.FieldSurveys.Any(s => s.Status == Domain.Enums.SurveyStatus.Submitted)) score += 0.10;

        // Address completeness
        maxScore += 0.20;
        var addressFields = new[] { property.StreetAddress, property.District, property.City, property.Governorate };
        score += 0.05 * addressFields.Count(f => !string.IsNullOrEmpty(f));

        return maxScore > 0 ? score / maxScore : 0.0;
    }

    public async Task<IEnumerable<DuplicateCandidate>> DetectDuplicatesAsync(Property property, CancellationToken ct = default)
    {
        var candidates = new List<DuplicateCandidate>();

        // TODO: Implement spatial duplicate search using PostGIS ST_DWithin
        // For now, use basic attribute matching
        var potential = await _db.Properties
            .Where(p => p.Id != property.Id
                && !p.IsDeleted
                && p.Governorate == property.Governorate
                && p.City == property.City)
            .Take(50)
            .ToListAsync(ct);

        foreach (var candidate in potential)
        {
            var similarityScore = CalculateAttributeSimilarity(property, candidate);
            if (similarityScore >= 0.70)
            {
                var matchingAttrs = GetMatchingAttributes(property, candidate);
                candidates.Add(new DuplicateCandidate(candidate.Id, candidate.PropertyCode, similarityScore, matchingAttrs));
            }
        }

        return candidates.OrderByDescending(c => c.SimilarityScore);
    }

    private async Task<Guid?> FindByNationalIdAsync(string nationalId, CancellationToken ct)
    {
        var ownership = await _db.PropertyOwnerships
            .Where(o => o.Taxpayer.NationalId == nationalId && o.IsCurrent && !o.IsDeleted)
            .Select(o => (Guid?)o.PropertyId)
            .FirstOrDefaultAsync(ct);
        return ownership;
    }

    private async Task<Guid?> FindByGisAsync(double lat, double lng, CancellationToken ct)
    {
        // TODO: Replace with PostGIS ST_DWithin query for accurate spatial search
        var tolerance = GisMatchRadiusMeters / 111000.0; // rough degree conversion
        var location = await _db.PropertyLocations
            .Where(l => !l.IsDeleted
                && l.Latitude.HasValue && l.Longitude.HasValue
                && Math.Abs(l.Latitude!.Value - lat) < tolerance
                && Math.Abs(l.Longitude!.Value - lng) < tolerance)
            .Select(l => (Guid?)l.PropertyId)
            .FirstOrDefaultAsync(ct);
        return location;
    }

    private async Task<Guid?> FindByAddressSimilarityAsync(string address, CancellationToken ct)
    {
        // TODO: Replace with PostgreSQL trigram similarity (pg_trgm) for production quality matching
        var normalised = address.ToUpperInvariant().Trim();
        var potential = await _db.Properties
            .Where(p => !p.IsDeleted && p.FullAddress != null)
            .Select(p => new { p.Id, p.FullAddress })
            .Take(100)
            .ToListAsync(ct);

        foreach (var p in potential)
        {
            if (p.FullAddress == null) continue;
            var sim = ComputeSimpleSimilarity(normalised, p.FullAddress.ToUpperInvariant());
            if (sim >= AddressSimilarityThreshold) return p.Id;
        }
        return null;
    }

    private async Task<Guid?> FindByMeterNumberAsync(string? meterNumber, string? accountNumber, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(meterNumber) && string.IsNullOrEmpty(accountNumber)) return null;

        var record = await _db.PropertySourceRecords
            .Where(r => r.IsMatched && r.MasterPropertyId != null && !r.IsDeleted
                && ((!string.IsNullOrEmpty(meterNumber) && r.MeterNumber == meterNumber)
                    || (!string.IsNullOrEmpty(accountNumber) && r.AccountNumber == accountNumber)))
            .Select(r => r.MasterPropertyId)
            .FirstOrDefaultAsync(ct);
        return record;
    }

    private static double CalculateAttributeSimilarity(Property a, Property b)
    {
        double score = 0.0;
        int checks = 0;

        if (!string.IsNullOrEmpty(a.StreetAddress) && !string.IsNullOrEmpty(b.StreetAddress))
        {
            score += ComputeSimpleSimilarity(a.StreetAddress, b.StreetAddress);
            checks++;
        }
        if (a.BuiltUpArea > 0 && b.BuiltUpArea > 0)
        {
            score += 1.0 - Math.Min(1.0, Math.Abs((double)(a.BuiltUpArea - b.BuiltUpArea)) / (double)a.BuiltUpArea);
            checks++;
        }
        if (a.YearBuilt.HasValue && b.YearBuilt.HasValue)
        {
            score += a.YearBuilt == b.YearBuilt ? 1.0 : 0.0;
            checks++;
        }

        return checks > 0 ? score / checks : 0.0;
    }

    private static string[] GetMatchingAttributes(Property a, Property b)
    {
        var attrs = new List<string>();
        if (a.Governorate == b.Governorate) attrs.Add("Governorate");
        if (a.City == b.City) attrs.Add("City");
        if (a.District == b.District) attrs.Add("District");
        if (a.YearBuilt == b.YearBuilt) attrs.Add("YearBuilt");
        return [.. attrs];
    }

    private static double ComputeSimpleSimilarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
        // Simple Jaccard similarity on trigrams — use SimMetrics.Net or pg_trgm in production
        var setA = GetTrigrams(a);
        var setB = GetTrigrams(b);
        var intersection = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();
        return union > 0 ? (double)intersection / union : 0.0;
    }

    private static HashSet<string> GetTrigrams(string s)
    {
        var result = new HashSet<string>();
        s = s.Replace(" ", "").ToLowerInvariant();
        for (int i = 0; i < s.Length - 2; i++)
            result.Add(s.Substring(i, 3));
        return result;
    }
}
