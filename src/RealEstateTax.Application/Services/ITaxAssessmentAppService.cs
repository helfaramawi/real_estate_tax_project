using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.TaxAssessments;

namespace RealEstateTax.Application.Services;

public interface ITaxAssessmentAppService
{
    Task<Result<TaxAssessmentDto>> GenerateAsync(GenerateTaxAssessmentRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<TaxAssessmentDto>>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<Result<TaxAssessmentDto>> ApproveAsync(Guid id, ApproveTaxAssessmentRequest request, CancellationToken ct = default);
}
