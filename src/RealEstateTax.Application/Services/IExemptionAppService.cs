using RealEstateTax.Application.Common.Models;
using RealEstateTax.Application.DTOs.Exemptions;

namespace RealEstateTax.Application.Services;

public interface IExemptionAppService
{
    Task<Result<ExemptionDto>> SubmitAsync(SubmitExemptionRequest request, CancellationToken ct = default);
    Task<Result<IEnumerable<ExemptionDto>>> GetByPropertyAsync(Guid propertyId, CancellationToken ct = default);
    Task<Result<ExemptionDto>> ApproveAsync(Guid id, ApproveExemptionRequest request, CancellationToken ct = default);
    Task<Result<ExemptionDto>> RejectAsync(Guid id, RejectExemptionRequest request, CancellationToken ct = default);
}
