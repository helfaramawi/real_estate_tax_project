using RealEstateTax.Domain.Entities;

namespace RealEstateTax.Domain.Services;

public interface IPropertyMatchingService
{
    /// <summary>
    /// Attempts to match a source record against existing master properties using
    /// GIS coordinates, address similarity, owner national ID, and meter numbers.
    /// </summary>
    Task<MatchResult> FindMatchAsync(PropertySourceRecord sourceRecord, CancellationToken ct = default);

    /// <summary>
    /// Calculates overall confidence score for a property record based on
    /// data completeness, cross-source consistency, and GIS verification.
    /// </summary>
    Task<double> CalculateConfidenceScoreAsync(Property property, CancellationToken ct = default);

    /// <summary>
    /// Detects potential duplicate master records based on proximity and attribute similarity.
    /// </summary>
    Task<IEnumerable<DuplicateCandidate>> DetectDuplicatesAsync(Property property, CancellationToken ct = default);
}

public record MatchResult(
    bool IsMatch,
    Guid? MatchedPropertyId,
    double ConfidenceScore,
    string MatchMethod,
    string[] ContributingFactors
);

public record DuplicateCandidate(
    Guid CandidatePropertyId,
    string PropertyCode,
    double SimilarityScore,
    string[] MatchingAttributes
);
