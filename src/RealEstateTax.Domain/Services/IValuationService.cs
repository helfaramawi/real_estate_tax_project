using RealEstateTax.Domain.Entities;
using RealEstateTax.Domain.Enums;

namespace RealEstateTax.Domain.Services;

public interface IValuationService
{
    /// <summary>
    /// Computes the net taxable value for a property using the specified method.
    /// The rental value method is standard under Egyptian law.
    /// TODO: Obtain official rate tables from Egyptian Tax Authority for rental value zones.
    /// </summary>
    Task<ValuationResult> ComputeValueAsync(
        Property property,
        ValuationMethod method,
        ValuationRule rule,
        CancellationToken ct = default);
}

public record ValuationResult(
    decimal GrossValue,
    decimal DeductionPercentage,
    decimal NetValue,
    string MethodUsed,
    string[] Assumptions,
    string[] Warnings
);
